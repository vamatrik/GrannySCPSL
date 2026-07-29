using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayerRoles;
using MEC;
using Mirror;
using LabApi.Features.Wrappers;
using GrannySCPSL.Graph;
using PlayerRoles.FirstPersonControl;

namespace GrannySCPSL.AI
{
    public class GrannyAI
    {
        public static GrannyAI Instance { get; } = new GrannyAI();

        public enum AIState
        {
            Patrol,
            Investigate,
            Chase,
            Searching,
            Stunned
        }

        public AIState CurrentState { get; private set; } = AIState.Patrol;
        public bool IsCutscene = false;
        public bool hasBeenStunned = false;
        public GameObject? Dummy { get; private set; }
        public ReferenceHub? dummyHub;
        public Player? grannyPlayer;
        
        private Node? currentTargetNode;
        private Node? previousNode;
        private Queue<Node> currentPath = new Queue<Node>();
        
        private CoroutineHandle aiCoroutine;
        private float timeStuck = 0f;
        private float searchAngle = 0f;
        private Vector3 initialSpawnPoint;

        public void SpawnGranny(Vector3 spawnPosition)
        {
            initialSpawnPoint = spawnPosition;
            var assembly = typeof(ServerConsole).Assembly;
            var dummyUtilsType = System.Linq.Enumerable.FirstOrDefault(assembly.GetTypes(), t => t.Name == "DummyUtils");
            
            if (dummyUtilsType != null)
            {
                var method = dummyUtilsType.GetMethod("SpawnDummy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    dummyHub = (ReferenceHub)method.Invoke(null, new object[] { "Granny SCP-939" });
                }
            }
            else
            {
                LabApi.Features.Console.Logger.Error("Could NOT find DummyUtils via reflection!");
            }
            
            if (dummyHub == null)
            {
                LabApi.Features.Console.Logger.Error("dummyHub is NULL! SpawnDummy failed.");
                return;
            }
            Dummy = dummyHub.gameObject;
            
            dummyHub.roleManager.ServerSetRole(RoleTypeId.Scp939, RoleChangeReason.RoundStart);
            
            grannyPlayer = Player.Get(Dummy);
            if (grannyPlayer != null)
            {
                grannyPlayer.Position = spawnPosition;
                try 
                {
                    grannyPlayer.CustomInfo = "Granny";
                    dummyHub.nicknameSync.Network_myNickSync = "Granny";
                    
                    if (dummyHub.roleManager.CurrentRole is PlayerRoles.FirstPersonControl.IFpcRole fpc)
                    {
                        fpc.FpcModule.CharController.enabled = false; // Disable physics completely
                    }
                } catch { }
            }
            
            CurrentState = AIState.Patrol;
            currentTargetNode = GetClosestNode(spawnPosition);
            previousNode = null;
            
            aiCoroutine = Timing.RunCoroutine(AITick());
            LabApi.Features.Console.Logger.Info("Granny (939) Spawned!");
        }

        public void DespawnGranny()
        {
            if (Dummy != null)
            {
                Timing.KillCoroutines(aiCoroutine);
                NetworkServer.Destroy(Dummy);
                Dummy = null;
                dummyHub = null;
                grannyPlayer = null;
            }
        }

        public void HearNoise(Vector3 noisePosition, Player noiseMaker = null)
        {
            if (noiseMaker != null) {
                try { noiseMaker.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Scanned>(5f, false); } catch { }
            }
            if (CurrentState == AIState.Chase || Dummy == null || dummyHub == null) return; 
            
            var targetNode = GetClosestNode(noisePosition);
            var startNode = GetClosestNode(dummyHub.transform.position);
            
            if (startNode != null && targetNode != null)
            {
                var path = GraphManager.Instance.GetPath(startNode, targetNode);
                if (path.Count > 0)
                {
                    currentPath = new Queue<Node>(path);
                    CurrentState = AIState.Investigate;
                    LabApi.Features.Console.Logger.Debug("Granny heard a noise and is pathfinding!");
                }
            }
        }

        private IEnumerator<float> AITick()
        {
            float freezeTimer = 15.0f;
            bool wokeUp = false;
            float lastAttackTime = 0f;
            float attackCooldownTimer = 0f;
            float doorCheckTimer = 0f;
            float investigateWaitTimer = 0f;
            float fakeFootstepTimer = Core.EventManager.FastGrenades ? 2f : UnityEngine.Random.Range(7f, 15f);
            Vector3 lastTargetPos = Vector3.zero;

            while (Dummy != null && dummyHub != null && grannyPlayer != null)
            {
                yield return Timing.WaitForOneFrame; // ~60 Hz server tick
                float dt = Time.deltaTime;
                Vector3 lastPos = dummyHub.transform.position;
                
                if (IsCutscene) { yield return Timing.WaitForOneFrame; continue; }
                  
                  if (Core.EventManager.FastGrenades && fakeFootstepTimer > 2f) {
                      fakeFootstepTimer = 2f;
                  }
                  
                  if (freezeTimer > 0)
                {
                    freezeTimer -= dt;
                    if (freezeTimer <= 0 && !wokeUp) {
                        wokeUp = true;
                        LabApi.Features.Wrappers.Server.SendBroadcast(Core.TranslationManager.GetString("granny_woke_up", null), 4, shouldClearPrevious: true);
                    }
                    continue;
                }
                
                fakeFootstepTimer -= dt;
                if (fakeFootstepTimer <= 0f)
                {
                    fakeFootstepTimer = Core.EventManager.FastGrenades ? 2f : UnityEngine.Random.Range(7f, 15f);
                    if (dummyHub != null && grannyPlayer != null)
                    {
                        try 
                        {
                            if (dummyHub.roleManager.CurrentRole is PlayerRoles.PlayableScps.Scp939.Scp939Role scp939)
                            {
                                if (scp939.SubroutineModule.TryGetSubroutine<PlayerRoles.PlayableScps.Scp939.Mimicry.EnvironmentalMimicry>(out var mimicry))
                                {
                                    var mimicryType = mimicry.GetType();
                                    var syncOptionField = mimicryType.GetField("_syncOption", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                                    if (syncOptionField != null)
                                    {
                                        try {
                                            if (syncOptionField.FieldType.IsEnum) {
                                                ItemType[] validItems = new ItemType[] { 
                                                    ItemType.GunAK, ItemType.GunCOM15, ItemType.GunCOM18, ItemType.GunCrossvec, 
                                                    ItemType.GunE11SR, ItemType.GunFSP9, ItemType.GunLogicer, ItemType.GunRevolver, 
                                                    ItemType.GunShotgun, ItemType.Medkit, ItemType.Painkillers, 
                                                    ItemType.SCP207, ItemType.SCP018, ItemType.None, ItemType.None, ItemType.None, 
                                                    ItemType.None, ItemType.None, ItemType.GrenadeHE, ItemType.SCP1344
                                                };
                                                int randomIdx = UnityEngine.Random.Range(0, validItems.Length);
                                                syncOptionField.SetValue(mimicry, System.Enum.ToObject(syncOptionField.FieldType, (int)validItems[randomIdx]));
                                            } else {
                                                syncOptionField.SetValue(mimicry, (byte)UnityEngine.Random.Range(0, 30));
                                            }
                                        } catch { }
                                        
                                        var methods = mimicryType.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
                                        System.Reflection.MethodInfo sendRpcMethod = null;
                                        foreach (var m in methods) {
                                            if (m.Name == "ServerSendRpc") {
                                                sendRpcMethod = m;
                                                break;
                                            }
                                        }
                                        
                                        if (sendRpcMethod == null && mimicryType.BaseType != null) {
                                            var baseMethods = mimicryType.BaseType.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
                                            foreach (var m in baseMethods) {
                                                if (m.Name == "ServerSendRpc") {
                                                    sendRpcMethod = m;
                                                    break;
                                                }
                                            }
                                        }
                                        
                                        if (sendRpcMethod != null)
                                        {
                                            var pars = sendRpcMethod.GetParameters();
                                            if (pars.Length == 1 && pars[0].ParameterType == typeof(bool)) {
                                                sendRpcMethod.Invoke(mimicry, new object[] { true });
                                            } else if (pars.Length == 0) {
                                                sendRpcMethod.Invoke(mimicry, null);
                                            } else if (pars.Length == 1) {
                                                // Just in case it's something else we can try null
                                                sendRpcMethod.Invoke(mimicry, new object[] { null });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            LabApi.Features.Console.Logger.Error("[GrannyAI] Error during 939 mimicry: " + ex.ToString());
                        }
                    }
                }
                
                doorCheckTimer -= dt;
                if (doorCheckTimer <= 0)
                {
                    doorCheckTimer = 0.5f;
                    foreach (var door in LabApi.Features.Wrappers.Door.List)
                    {
                        if (door.IsOpened || door.IsLocked || (int)door.Permissions != 0) continue;
                        if (Vector3.Distance(dummyHub.transform.position, door.Position) < 2.5f)
                            door.IsOpened = true;
                    }
                }
                
                Player? target = null;
                float closestDist = float.MaxValue;
                if (attackCooldownTimer > 0) attackCooldownTimer -= dt;
                  
                  foreach (var p in Player.GetAll().Where(p => p.IsAlive && p.GameObject != Dummy && p.Role != RoleTypeId.Tutorial))
                  {
                      float dist = Vector3.Distance(dummyHub.transform.position, p.Position);
                      
                      bool isInvisible = false;
                      var invEffect = p.ReferenceHub.playerEffectsController.GetEffect<CustomPlayerEffects.Invisible>();
                      if (invEffect != null && invEffect.Intensity > 0) isInvisible = true;

                        if (!isInvisible && dist < 2.2f && Time.time - lastAttackTime > 3.0f)
                        {
                              lastAttackTime = Time.time;
                              attackCooldownTimer = 4.0f;
                              p.Damage(65f, Core.TranslationManager.GetString("death_granny", p));
                            try {
                              p.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Blurred>(1.5f, true);
                          } catch { }
                      }
                      
                      int pid = p.PlayerId;
                      if (!Core.GameManager.playerLastPos.ContainsKey(pid)) 
                      {
                          Core.GameManager.playerLastPos[pid] = p.Position;
                          Core.GameManager.playerStillTime[pid] = 0f;
                      }
                      
                      float distMoved = Vector3.Distance(Core.GameManager.playerLastPos[pid], p.Position);
                      float currentSpeed = distMoved / dt;
                      
                      if (currentSpeed < 0.5f)
                      {
                          // GameManager will increment still time. We just use it for speed here.
                      }
                      else
                      {
                          // GameManager handles reset.
                      }
                      
                      float detectRadius = 5.0f;
                        if (currentSpeed < 2.5f) {
                            detectRadius = 3.0f;
                        }
                        
                        if (CurrentState == AIState.Investigate || CurrentState == AIState.Searching || CurrentState == AIState.Chase)
                        {
                            detectRadius += 3.5f;
                        }
                        
                        bool isHiding = false;
                        try {
                            var hideEffect = p.ReferenceHub.playerEffectsController.GetEffect<CustomPlayerEffects.Sinkhole>();
                            if (hideEffect != null && hideEffect.Intensity > 0) isHiding = true;
                        } catch { }
                        
                        if (isHiding) detectRadius = 1.5f;
                        if (isInvisible) detectRadius = 0f;
                      
                      if (!isInvisible && dist < detectRadius)
                      {
                          if (Physics.Linecast(dummyHub.transform.position + Vector3.up, p.Position + Vector3.up, out RaycastHit hit, PlayerRoles.PlayerRolesUtils.AttackMask))
                        {
                            if (hit.collider.transform.root != p.GameObject.transform.root)
                                continue;
                        }

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            target = p;
                        }
                    }
                }

                if (target != null)
                {
                    CurrentState = AIState.Chase;
                    
                    bool clearPath = !Physics.Linecast(dummyHub.transform.position + Vector3.up * 0.1f, target.Position + Vector3.up * 0.1f, LayerMask.GetMask("Default", "Glass"));
                    
                    if (clearPath && Vector3.Distance(dummyHub.transform.position, target.Position) < 7f)
                    {
                        currentPath.Clear();
                        lastTargetPos = target.Position;
                        Vector3 targetCameraPos = target.ReferenceHub.PlayerCameraReference != null ? target.ReferenceHub.PlayerCameraReference.position : target.Position + Vector3.up * 1.5f;
                        
                        float currentSpeed = (attackCooldownTimer > 0) ? (7.2f * 0.2f) : 7.2f;
                        MoveTowards(target.Position, currentSpeed, dt, targetCameraPos);
                        timeStuck = 0f;
                    }
                    else
                    {
                        if (Vector3.Distance(lastTargetPos, target.Position) > 2.0f || currentPath.Count == 0)
                        {
                            var startNode = GetClosestNode(dummyHub.transform.position);
                            var endNode = GetClosestNode(target.Position);
                            if (startNode != null && endNode != null)
                            {
                                currentPath = new Queue<Node>(GraphManager.Instance.GetPath(startNode, endNode));
                                lastTargetPos = target.Position;
                            }
                        }
                        
                        if (currentPath.Count > 0)
                        {
                            var nextNode = currentPath.Peek();
                            Vector3 targetPos = new Vector3(nextNode.X, nextNode.Y, nextNode.Z);
                            
                            Vector3 targetCameraPos = target.ReferenceHub.PlayerCameraReference != null ? target.ReferenceHub.PlayerCameraReference.position : target.Position + Vector3.up * 1.5f;
                            
                            float currentSpeed = (attackCooldownTimer > 0) ? (7.2f * 0.2f) : 7.2f;
                            MoveTowards(targetPos, currentSpeed, dt, targetCameraPos);
                            
                            if (Vector3.Distance(dummyHub.transform.position, targetPos) < 0.6f)
                                currentPath.Dequeue();
                                
                            if (Vector3.Distance(dummyHub.transform.position, lastPos) < 0.05f)
                                timeStuck += dt;
                            else
                                timeStuck = 0f;
                                
                            if (timeStuck > 0.5f)
                            {
                                currentPath.Dequeue();
                                timeStuck = 0f;
                            }
                        }
                    }
                }
                else
                {
                    if (CurrentState == AIState.Chase)
                    {
                        CurrentState = AIState.Investigate;
                        var startNode = GetClosestNode(dummyHub.transform.position);
                        var endNode = GetClosestNode(lastTargetPos);
                        currentPath.Clear();
                        if (startNode != null && endNode != null)
                        {
                            var path = GraphManager.Instance.GetPath(startNode, endNode);
                            foreach (var p in path) currentPath.Enqueue(p);
                        }
                        timeStuck = 0f;
                    }
                    
                    if (CurrentState == AIState.Investigate)
                    {
                        if (currentPath.Count > 0)
                        {
                            var nextNode = currentPath.Peek();
                            Vector3 targetPos = new Vector3(nextNode.X, nextNode.Y, nextNode.Z);
                              
                            if (Vector3.Distance(dummyHub.transform.position, targetPos) < 0.6f)
                                currentPath.Dequeue();
                            else
                            {
                                float currentSpeed = (attackCooldownTimer > 0) ? (7.2f * 0.2f) : 7.2f;
                                MoveTowards(targetPos, currentSpeed, dt);
                                  
                                if (Vector3.Distance(dummyHub.transform.position, lastPos) < 0.05f)
                                    timeStuck += dt;
                                else
                                    timeStuck = 0f;
                                      
                                if (timeStuck > 0.5f)
                                {
                                    currentPath.Dequeue();
                                    timeStuck = 0f;
                                }
                            }
                        }
                        else
                          {
                              CurrentState = AIState.Searching;
                              investigateWaitTimer = 3.0f;
                              searchAngle = 0f;
                          }
                      }
                      else if (CurrentState == AIState.Searching)
                      {
                          if (investigateWaitTimer > 0)
                          {
                              investigateWaitTimer -= dt;
                              searchAngle += (360f / 3.0f) * dt; // 360 degrees in 3.0 seconds
                            
                            Vector3 lookRot = Quaternion.Euler(0, searchAngle, 0) * Vector3.forward;
                            MoveTowards(dummyHub.transform.position, 0, dt, dummyHub.transform.position + lookRot);
                        }
                        else
                        {
                            CurrentState = AIState.Patrol;
                            currentTargetNode = GetClosestNode(dummyHub.transform.position);
                            previousNode = null;
                        }
                    }
                    else if (CurrentState == AIState.Patrol)
                    {
                        if (currentTargetNode != null)
                        {
                            Vector3 targetPos = new Vector3(currentTargetNode.X, currentTargetNode.Y, currentTargetNode.Z);
                            if (Vector3.Distance(dummyHub.transform.position, targetPos) < 0.6f)
                            {
                                timeStuck = 0f;
                                if (currentTargetNode.ConnectedNodeIds.Count > 0)
                                {
                                    int nextId = ChooseNextNode(currentTargetNode, previousNode);
                                    previousNode = currentTargetNode;
                                    currentTargetNode = GraphManager.Instance.Nodes.FirstOrDefault(n => n.Id == nextId);
                                }
                            }
                            else
                            {
                                float currentSpeed = (attackCooldownTimer > 0) ? (4.3f * 0.2f) : 4.3f;
                                MoveTowards(targetPos, currentSpeed, dt);
                                
                                if (Vector3.Distance(dummyHub.transform.position, lastPos) < 0.05f)
                                    timeStuck += dt;
                                else
                                    timeStuck = 0f;
                                    
                                if (timeStuck >= 0.5f)
                                {
                                    if (currentTargetNode.ConnectedNodeIds.Count > 0)
                                    {
                                        int nextId = ChooseNextNode(currentTargetNode, previousNode);
                                        previousNode = currentTargetNode;
                                        currentTargetNode = GraphManager.Instance.Nodes.FirstOrDefault(n => n.Id == nextId);
                                    }
                                    timeStuck = 0f;
                                }
                            }
                        }
                    }
                    // Random patrol pathfinding
                    if (CurrentState == AIState.Patrol)
                    {
                        if (currentPath.Count == 0 && currentTargetNode != null)
                        {
                            var randomNode = GraphManager.Instance.Nodes[UnityEngine.Random.Range(0, GraphManager.Instance.Nodes.Count)];
                            var path = GraphManager.Instance.GetPath(currentTargetNode, randomNode);
                            if (path.Count > 0)
                            {
                                currentPath = new Queue<Node>(path);
                            }
                        }
                    }
                    
                    // Hearing Check
                    if (CurrentState != AIState.Chase && freezeTimer <= 0 && !Core.EventManager.BadHearing)
                    {
                        foreach (var p in Player.GetAll())
                        {
                            if (p == grannyPlayer || p.Role == RoleTypeId.Spectator || p.Role == RoleTypeId.None) continue;
                            if (Vector3.Distance(dummyHub.transform.position, p.Position) < (Core.EventManager.GoodHearing ? 200f : 35f))
                            {
                                if (p.ReferenceHub.roleManager.CurrentRole is PlayerRoles.FirstPersonControl.IFpcRole fpcRole)
                                {
                                    if (fpcRole.FpcModule.CurrentMovementState == PlayerRoles.FirstPersonControl.PlayerMovementState.Sprinting)
                                    {
                                        HearNoise(p.Position, p);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private int ChooseNextNode(Node current, Node? previous)
        {
            var options = current.ConnectedNodeIds.ToList();
            if (previous != null && options.Count > 1)
                options.Remove(previous.Id);
            
            return options[UnityEngine.Random.Range(0, options.Count)];
        }

        public void Stun()
        {
            if (CurrentState == AIState.Stunned) return;
            CurrentState = AIState.Stunned;
            Timing.RunCoroutine(StunCoroutine());
        }

        private IEnumerator<float> StunCoroutine()
        {
            Vector3 dropPos = dummyHub != null ? dummyHub.transform.position : initialSpawnPoint;
            
            if (grannyPlayer != null)
            {
                var oldDummy = Dummy;
                grannyPlayer.Kill("Stunned (TX)");
                dummyHub = null;
                grannyPlayer = null;
                Dummy = null;
                
                if (oldDummy != null)
                {
                    Timing.CallDelayed(0.5f, () => {
                        if (oldDummy != null) Mirror.NetworkServer.Destroy(oldDummy);
                    });
                }
            }

            var smellItem = LabApi.Features.Wrappers.Pickup.Create(ItemType.SCP1853, dropPos);
            smellItem.Spawn();
            Core.ItemManager.GrannySmellSerials.Add(smellItem.Serial);

            LabApi.Features.Wrappers.Server.SendBroadcast(Core.TranslationManager.GetString("granny_stunned", null), 10, shouldClearPrevious: true);
            LabApi.Features.Wrappers.Announcer.Message("scp 9 3 9 has been neutralized for 40 seconds", "", false, 0f, 1f);

            yield return Timing.WaitForSeconds(40f);

            foreach (var ragdoll in UnityEngine.Object.FindObjectsOfType<PlayerRoles.Ragdolls.BasicRagdoll>())
            {
                if (ragdoll.Info.RoleType == RoleTypeId.Scp939)
                {
                    Mirror.NetworkServer.Destroy(ragdoll.gameObject);
                }
            }

            SpawnGranny(initialSpawnPoint);
            CurrentState = AIState.Patrol;
        }

        private void MoveTowards(Vector3 position, float speed, float dt, Vector3? lookAtPosition = null)
        {
            if (dummyHub == null || grannyPlayer == null) return;
            
            speed *= Core.EventManager.GrannySpeedMultiplier;
            
            // 1. Move perfectly towards 3D coordinate (ignoring physics entirely)
            Vector3 nextPos = Vector3.MoveTowards(dummyHub.transform.position, position, speed * dt);
            grannyPlayer.Position = nextPos;
            
            Vector3 rotDir;
            if (lookAtPosition.HasValue)
            {
                Vector3 flatTarget = lookAtPosition.Value;
                flatTarget.y = dummyHub.transform.position.y;
                rotDir = (flatTarget - dummyHub.transform.position).normalized;
            }
            else
            {
                Vector3 direction = (position - dummyHub.transform.position);
                direction.y = 0;
                rotDir = direction.normalized;
            }
            
            if (rotDir.sqrMagnitude > 0.001f)
            {
                var q = Quaternion.LookRotation(rotDir);
                if (dummyHub.roleManager.CurrentRole is IFpcRole fpc)
                {
                    fpc.FpcModule.MouseLook.CurrentHorizontal = q.eulerAngles.y;
                    dummyHub.TryOverrideRotation(new Vector2(0f - q.eulerAngles.x, q.eulerAngles.y));
                }
            }
        }

        private Node? GetClosestNode(Vector3 pos)
        {
            return GraphManager.Instance.Nodes
                .OrderBy(n => Vector3.Distance(new Vector3(n.X, n.Y, n.Z), pos))
                .FirstOrDefault();
        }
    }
}





