# Prison Break Game

**Version 0.2.0** - Multiplayer Networking System

A 2D top-down prison escape game built with **MonoGame** and **Entity Component System (ECS)** architecture, featuring complete real-time multiplayer support.

## Features

### 🎮 Core Gameplay
- **Player Types**: Choose between Prisoner (fast, 3 inventory slots) or Cop (standard speed, 4 inventory slots)
- **AI Cops**: Intelligent patrol system with collision detection
- **Interactive World**: Pick up items and interact with chests using E key or gamepad X button
- **Inventory System**: Visual slot-based inventory with drag-and-drop support
- **Collision System**: Player-cop collision handling with position reset

### 🌐 Multiplayer System (v0.2.0)
- **Real-time Multiplayer**: 2-8 player support with authoritative host architecture
- **Lobby System**: Create/join games with character selection and ready-up system
- **Player Synchronization**: 20Hz position updates for smooth movement
- **AI Synchronization**: Shared AI cop behavior across all players (10Hz updates)
- **Inventory Networking**: Authoritative item pickups and chest transfers
- **State Consistency**: Complete inventory state synchronization for reliability

### 🏗️ Technical Architecture
- **Entity Component System**: Clean, modular ECS architecture for game logic
- **Scene Management**: StartMenuScene, MultiplayerLobbyScene, and GameplayScene
- **Event-Driven Design**: Comprehensive EventBus system for component communication
- **Network Integration**: LiteNetLib-based networking with host authority model
- **Content Pipeline**: MonoGame Content Pipeline for assets and font management

## Quick Start

### Single Player
1. Run the game executable
2. Select "Start Game" from the main menu
3. Choose your player type (Prisoner or Cop)
4. Use WASD or arrow keys to move
5. Press E to interact with items and chests
6. Press ESC to return to menu

### Multiplayer
1. **Host a Game**:
   - Select "Multiplayer" → "Host Game"
   - Choose your character type
   - Wait for players to join
   - Click "Ready" when ready to start

2. **Join a Game**:
   - Select "Multiplayer" → "Join Game"
   - Enter the host's IP address
   - Choose your character type
   - Click "Ready" when ready to play

## Controls

### Gameplay
- **Movement**: WASD keys or Arrow keys
- **Interact**: E key or Gamepad X button
- **Menu**: ESC key

### Inventory (Chest Interface)
- **Navigate Slots**: Arrow keys
- **Switch Inventories**: Up/Down arrows (player ↔ chest)
- **Transfer Items**: Enter key or Gamepad A button
- **Close Chest**: ESC key or Gamepad B button

### Menu Navigation
- **Navigate**: Arrow keys or D-pad
- **Select**: Enter key or Gamepad A button
- **Back**: ESC key or Gamepad B button

## System Requirements

- **.NET 9.0** or later
- **MonoGame Framework** 3.8+
- **Windows, macOS, or Linux**
- **OpenGL-compatible graphics**

### Multiplayer Requirements
- **Local Network**: Same WiFi/LAN for local multiplayer
- **Internet**: Direct IP connection support
- **Ports**: Default port 9050 (configurable)

## Technical Details

### Networking Architecture
- **Protocol**: UDP via LiteNetLib
- **Authority Model**: Host-authoritative for security and consistency
- **Update Rates**: 20Hz player positions, 10Hz AI behavior
- **Bandwidth**: <1KB/s per player typical usage

### Performance
- **Target FPS**: 60 FPS
- **Memory Usage**: <100MB typical
- **Startup Time**: <5 seconds
- **Network Latency**: <100ms LAN, <200ms internet

## Development

### Building from Source
```bash
# Clone the repository
git clone <repository-url>
cd PrisonBreak

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the game
dotnet run --project PrisonBreak
```

### Project Structure
```
PrisonBreak/                    # Main game project
├── ECS/                        # Entity Component System
├── Scenes/                     # Scene management
├── Managers/                   # Game managers (Network, System)
├── Core/                       # Core systems (Graphics, Input, Math)
└── Content/                    # Game assets

PrisonBreak.Multiplayer/        # Networking library
├── Core/                       # Network infrastructure
└── Messages/                   # Network message definitions
```

## Architecture Documentation

- **[Multiplayer Specification](MULTIPLAYER_SPEC.md)**: Complete technical documentation of the multiplayer system
- **[Changelog](CHANGELOG.md)**: Detailed version history and feature additions
- **[ECS Reference](_memory/ECS_QUICK_REFERENCE.md)**: Entity Component System documentation
- **[Future Plans](_memory/futurePlans.md)**: Roadmap and upcoming features

## Contributing

This is a learning/demonstration project showcasing modern game development patterns:
- Clean ECS architecture
- Event-driven design patterns
- Authoritative multiplayer networking
- Scene-based game state management

## License

Educational/demonstration project - see project files for details.

---

**Prison Break Game v0.2.0** - Real-time multiplayer prison escape simulation with authoritative networking architecture.