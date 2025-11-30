# Meyz's Toolbag

**Meyz’s Toolbag** is a Unity **Editor toolbox** that collects multiple quality-of-life tools into a single, centralized window.

The aim is simple: instead of scattering small utility scripts across different menus and windows, Meyz’s Toolbag provides **one hub** where you can discover, configure, and use your editor utilities in a consistent way.

This repository is also intended as a **portfolio / CV project** to showcase Unity Editor tooling, custom editor windows, and data-driven workflows.

---

## 🚀 Features

* **Centralized Toolbag Window**
  Access every tool from a single unified editor window.

* **Category-Based Organization**
  Tools can be grouped by category (*Scene*, *Transform*, *Animation*, *Utility*, *Performance*).

* **Extensible Architecture**
  Each tool is implemented as a modular component with minimal coupling.

* **ScriptableObject Settings (Optional)**
  Store presets and preferences in `Data/` as assets.

* **Editor/Runtime Separation**
  Clean assembly separation ensures editor-only code never enters builds.

* **Undo-Safe Operations**
  Tools use Unity’s Undo system where applicable.

---

## 📂 Project Structure

```text
meyzs-toolbag/
├─ Data/                  # ScriptableObject assets, tool settings, presets (optional)
├─ Editor/                # Editor-only code: toolbag window, individual tools, inspectors
│  ├─ Core/               # Core window, base tool interfaces, shared editor utilities
│  ├─ SceneTools/         # Scene editing / duplication helpers
│  ├─ Transform/          # Transform / pivot / alignment tools
│  ├─ Animation/          # Animation / pose helpers
│  ├─ Utility/            # Renamers, search tools, cleanup utilities
│  └─ Performance/        # Analysis / validation tools
├─ Runtime/               # Runtime components (if any)
└─ Resources/             # Icons, UI assets, default ScriptableObjects
```

---

## 🧰 Tools

### Scene / Level Design Tools

* Duplicate objects in patterns (grid, circle, line, random)
* Populate scenes efficiently
* Save / load scene snapshots
* Batch operations on large selections

### Transform & Pivot Tools

* Recenter or shift pivots based on bounds
* Align objects precisely
* Apply offsets, scaling, normalization in bulk

### Animation Tools

* Preview animation poses directly in the Scene
* Snap objects to specific animation frames
* Apply pose data to duplicates or previews

### Utility Tools

* Batch rename (objects, prefabs, assets)
* Quick material swapping
* Project-wide asset usage searching
* General validation helpers

### Performance / Validation Tools

* Detect missing scripts
* Validate scenes for common issues
* Prepare textures / atlases before build

---

## 🛠 Installation

1. Clone or download the repository:

   ```bash
   git clone https://github.com/byte-me-pls/meyzs-toolbag.git
   ```

2. Move the folder into your Unity project:

   ```text
   Assets/Tools/meyzs-toolbag/
   ```

3. Unity will automatically reimport the scripts. No additional setup is required.

---

## 🧭 Usage

1. Open **Meyz’s Toolbag** from the Unity menu, e.g.:

   * `Window ▸ Meyz's Toolbag`
   * or `Tools ▸ Meyz's Toolbag`

2. Browse categories like:

   * *Scene Tools*
   * *Transform*
   * *Animation*
   * *Utility*
   * *Performance*

3. Click a tool to:

   * Open a panel inside the Toolbag window, or
   * Run a direct action, depending on the tool.

4. Some tools use ScriptableObject settings found under `Data/`.

---

## 🎯 Project Purpose

This repository is built to demonstrate:

* Unity Editor API knowledge (`EditorWindow`, `MenuItem`, custom inspectors)
* IMGUI-based editor UI workflows
* Modular, extensible tool architecture
* ScriptableObject-driven design
* Real production-oriented editor utilities

It is primarily intended as a **portfolio / CV showcase**, not as a production-ready Unity Asset Store package.

---

## 🧩 Extending the Toolbag

To add a new tool:

1. Create a folder under `Editor/YourToolName/`.
2. Implement your tool (following the core interface / architecture).
3. Register the tool in the Toolbag window.
4. (Optional) Add ScriptableObject settings in `Data/`.

---

## 📄 License

license (MIT).

---

## 👤 Author

Developed by **byte-me-pls (meyz)**.
Focused on editor tooling, automation, and workflow optimization in Unity.
