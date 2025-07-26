# Architecture Decision Record: Phase 1 Multiplayer Architecture

**Date**: January 26, 2025  
**Status**: Accepted  
**Phase**: Phase 1 (Core Networking Infrastructure)

---

## Context

The Prison Break game requires multiplayer functionality to be added to an existing single-player ECS-based architecture. The system needs to integrate seamlessly with existing components, systems, and events while maintaining single-player compatibility and preparing for future phases.

## Decision Summary

We implemented a **dual-project architecture** with clean separation between pure networking and game integration, using **peer-to-peer networking** with **event-bridge patterns** for seamless integration.

---

## Architecture Decisions

### 1. Project Structure: Dual-Project Separation

**Decision**: Split networking code between two projects
- `PrisonBreak.Multiplayer`: Pure networking library (LiteNetLib integration only)
- `PrisonBreak`: Game integration (ECS components, systems, managers)

**Alternatives Considered**:
- Single project with all networking code in `PrisonBreak.Multiplayer`
- All networking code in main `PrisonBreak` project

**Rationale**: 
- **Problem Solved**: Eliminates circular dependencies that prevented compilation
- **Benefits**: Reusable networking library, cleaner separation of concerns
- **Future-Proof**: Networking library can be used by other projects

**Trade-offs**:
- ✅ **Pros**: Clean architecture, no circular dependencies, reusable components
- ❌ **Cons**: Slightly more complex project setup, need to maintain two projects

### 2. Networking Pattern: Peer-to-Peer (P2P)

**Decision**: Implement P2P networking with LocalHost/Client pattern

**Alternatives Considered**:
- Dedicated server architecture
- Hybrid P2P/server model

**Rationale**:
- **Simplicity**: Easier to implement and test for Phase 1
- **Deployment**: No need for dedicated server infrastructure
- **Cost**: No server hosting costs for players
- **Extensibility**: Can add dedicated server support in future phases

**Implementation**:
```csharp
public enum GameMode
{
    SinglePlayer,   // No networking
    LocalHost,      // Host + play locally  
    Client          // Connect to remote host
}
```

### 3. Component Integration: Selective NetworkComponent

**Decision**: Add NetworkComponent only to entities requiring network synchronization

**Alternatives Considered**:
- All entities automatically get NetworkComponent
- Separate networked and local entity managers

**Rationale**:
- **Performance**: Only sync entities that need it (walls don't need networking)
- **Clarity**: Explicit opt-in for network synchronization
- **Control**: Fine-grained control over what gets synchronized

**Implementation**:
```csharp
public struct NetworkComponent
{
    public int NetworkId;
    public NetworkConfig.NetworkAuthority Authority;
    public bool SyncTransform;   // Opt-in syncing
    public bool SyncMovement;
    public bool SyncInventory;
    public int OwnerId;
}
```

### 4. Event Integration: Event-Bridge Pattern

**Decision**: Always subscribe to events, filter by game mode in handlers

**Alternatives Considered**:
- Subscribe/unsubscribe based on game mode changes
- Separate event buses for single-player and multiplayer

**Rationale**:
- **Simplicity**: No subscription lifecycle management
- **Reliability**: No risk of missing subscription/unsubscription during mode changes  
- **Performance**: Minimal overhead - just an early return in single-player mode

**Implementation**:
```csharp
private void OnPlayerInput(PlayerInputEvent inputEvent)
{
    if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
        return;
    // Convert to network message and send
}
```

### 5. Message Design: Concrete MessageType Property

**Decision**: Use concrete property set in constructor instead of abstract property

**Alternatives Considered**:
- Abstract MessageType property requiring override in each message class
- Interface-only approach without base class

**Rationale**:
- **Simplicity**: Less boilerplate code in message classes
- **Maintainability**: Constructor approach is more straightforward
- **Consistency**: Follows existing component constructor patterns

**Implementation**:
```csharp
public abstract class NetworkMessage : INetworkMessage
{
    public NetworkConfig.MessageType Type { get; protected set; }
    
    protected NetworkMessage(NetworkConfig.MessageType type, int entityId = -1)
    {
        Type = type;
        EntityId = entityId;
    }
}
```

### 6. System Integration: IGameSystem Compliance

**Decision**: All networking systems implement existing IGameSystem interface

**Alternatives Considered**:
- Custom networking system lifecycle
- Direct integration without system manager

**Rationale**:
- **Consistency**: Follows established architectural patterns
- **Integration**: Works seamlessly with existing SystemManager
- **Lifecycle**: Automatic Initialize/Update/Shutdown management

**Implementation**:
```csharp
public class NetworkManager : IGameSystem
{
    public void SetEntityManager(ComponentEntityManager entityManager) { }
    public void SetEventBus(EventBus eventBus) { }
    public void Initialize() { }
    public void Update(GameTime gameTime) { }
    public void Draw(SpriteBatch spriteBatch) { }
    public void Shutdown() { }
}
```

---

## Implementation Impacts

### Positive Impacts

1. **Single-Player Compatibility**: Existing functionality unchanged
2. **Clean Architecture**: Clear separation of concerns 
3. **Performance**: Minimal overhead when networking disabled
4. **Extensibility**: Easy to add new message types and network features
5. **Testing**: Each layer can be tested independently
6. **Reusability**: Pure networking library can be used elsewhere

### Trade-offs Accepted

1. **Complexity**: Two projects to maintain instead of one
2. **Learning Curve**: Developers need to understand both projects
3. **P2P Limitations**: No centralized authority for complex game logic (addressed in future phases)

### Migration Path

- **Immediate**: Phase 1 provides foundation for basic networking
- **Phase 2**: Can add lobby system using established event patterns
- **Phase 3**: Can add dedicated server support by extending GameMode enum
- **Future**: Architecture supports advanced features like host migration

---

## Validation

### Success Criteria Met

✅ **Both projects build successfully** - No circular dependencies  
✅ **Single-player compatibility maintained** - Existing functionality unchanged  
✅ **Clean separation of concerns** - Pure networking vs game integration  
✅ **Extensible foundation** - Ready for Phase 2 implementation  
✅ **Consistent with existing patterns** - Follows ECS, event, and system patterns  

### Technical Validation

- **Build Tests**: Both `PrisonBreak` and `PrisonBreak.Multiplayer` compile without errors
- **Integration Tests**: NetworkManager integrates with existing SystemManager
- **Event Tests**: Event-bridge pattern works with existing EventBus
- **Component Tests**: NetworkComponent works with existing component queries

---

## Future Considerations

### Phase 2 Readiness
- ✅ Lobby system can use NetworkEventBus for player state
- ✅ Character selection can use ComponentMessages for sync
- ✅ Game mode transitions can use existing scene management

### Phase 3+ Extensions
- **Dedicated Servers**: Add `DedicatedServer` to GameMode enum
- **Advanced Authority**: Extend authority model for complex game logic
- **Performance**: Add message batching and compression
- **Security**: Add validation and anti-cheat measures

### Potential Refactoring Needs
- **Message Routing**: May need more sophisticated routing for complex scenarios
- **State Management**: May need state snapshots for late-joining clients
- **Error Handling**: May need more robust error recovery mechanisms

---

## Related Documents

- [Phase 1 Implementation Guide](Phase1.md) - Detailed implementation instructions
- [Phase 1 Implementation Status](Phase1_ImplementationStatus.md) - Current progress tracking
- [Multiplayer Plan](Plan.md) - Overall multiplayer roadmap
- [ECS Quick Reference](../ECS_QUICK_REFERENCE.md) - Updated with networking patterns

---

## Decision Ownership

**Primary Architect**: Claude (Assistant)  
**Validated By**: User feedback and iterative refinement  
**Implementation Team**: Collaborative development

This ADR captures the key architectural decisions that shaped Phase 1 of the multiplayer implementation and provides context for future development phases.