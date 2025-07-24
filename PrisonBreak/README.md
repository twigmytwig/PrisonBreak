# Prison Break Game

A 2D action game built with MonoGame featuring an Entity Component System (ECS) architecture, scene management, and comprehensive inventory system.

## 🎮 Current Features

### Core Gameplay

- **Player Types**: Choose between Prisoner and Cop with different attributes
  - Prisoner: 3 inventory slots, specific animations
  - Cop: 4 inventory slots, different movement characteristics
- **Scene-Based Architecture**: Clean separation between menu and gameplay
- **ECS Framework**: Robust entity-component-system for game objects

### 🎒 Inventory System (v0.1.2)

- **Visual Inventory UI**: Real-time inventory display during gameplay
- **Dynamic Slot System**: Inventory capacity varies by player type
- **Event-Driven**: ItemAddedEvent, ItemRemovedEvent, InventoryFullEvent
- **UI Atlas Integration**: Consistent visual design with PrisonBreakUI.png

### 🎬 Scene Management

- **StartMenuScene**: Professional menu with player type selection
- **GameplayScene**: Main game logic and systems
- **SceneManager**: Handles transitions and lifecycle management

### 🎨 UI System

- **Menu Navigation**: Arrow keys, Enter, ESC support
- **Font Integration**: Custom Minecraft font via Content Pipeline
- **Responsive Design**: UI adapts to different screen elements

## 🏗️ Architecture

### Entity Component System (ECS)

- **Components**: Data containers (Transform, Sprite, PlayerType, Inventory, etc.)
- **Systems**: Logic processors (Movement, Rendering, Input, Inventory, etc.)
- **Entities**: Game objects composed of components
- **EventBus**: Decoupled communication between systems

### Key Systems

- `ComponentMovementSystem` - Player movement and physics
- `ComponentRenderSystem` - Sprite and graphics rendering
- `ComponentInputSystem` - Keyboard and gamepad input
- `InventorySystem` - Core inventory management
- `InventoryUIRenderSystem` - Inventory visual interface
- `MenuInputSystem` - Menu navigation
- `MenuRenderSystem` - Menu display

## 🎯 Controls

### Menu Navigation

- **Arrow Keys**: Navigate menu options and select player type
- **Enter**: Confirm selection / Start game
- **ESC**: Return to menu from gameplay

### Gameplay

- **WASD / Arrow Keys**: Player movement
- **Gamepad Support**: Xbox controller compatible

## 🔧 Technical Details

### Built With

- **MonoGame Framework**: Cross-platform game development
- **C# .NET**: Primary programming language
- **Content Pipeline**: Asset management and processing

### Project Structure

```
/ECS/                    # Entity Component System
├── Components.cs        # All game components
├── Systems/            # Game logic systems
└── EventSystem.cs      # Event communication

/Scenes/                # Scene management
├── Scene.cs            # Base scene class
├── SceneManager.cs     # Scene lifecycle
├── StartMenuScene.cs   # Main menu
└── GameplayScene.cs    # Game logic

/Content/               # Game assets
├── images/             # Textures and UI
└── minecraft/          # Font assets
```

### Current Version: v0.1.2

## 🚀 Upcoming Features

The architecture is designed to support:

- **Item Interaction System**: Pickup and drop mechanics
- **Chest/Container System**: Interactive storage containers
- **Advanced Inventory Management**: Drag-and-drop, item stacking
- **Door Interaction System**: Level progression mechanics
- **Multiplayer Support**: Network-based gameplay

## 📋 Getting Started

### Prerequisites

- Visual Studio 2019+ or Visual Studio Code
- .NET 5.0 or later
- MonoGame Framework

### Building and Running

1. Clone the repository
2. Open `PrisonBreak.sln` in Visual Studio
3. Restore NuGet packages
4. Build and run the project

## 📈 Development Progress

See [CHANGELOG.md](CHANGELOG.md) for detailed version history and feature updates.

- ✅ v0.1.0 - Scene-based architecture
- ✅ v0.1.1 - Core inventory system
- ✅ v0.1.2 - Inventory UI system
- 🚧 v0.1.3 - Item interaction system (planned)

## 🤝 Contributing

This project follows clean architecture principles and ECS patterns. When contributing:

1. Follow existing code conventions
2. Use the component-system pattern for new features
3. Maintain event-driven communication
4. Update documentation for new features

## 📝 License

This project is for educational and development purposes.
