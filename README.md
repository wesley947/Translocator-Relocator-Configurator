# Translocator Relocator +Configurator

An enhanced version of **Translocator Relocator** for Vintage Story 1.22+, building upon the original mod by **Windows98** and the compatibility fork by **Tirello**.

**Mod ID:** `translocatorconfigurator`  
**Version:** 1.0.9  
**License:** MIT

## Features

Relocate repaired static translocators, connect them into named networks, and enjoy more control over your destinations.

**+Configurator enhancements:**

- **Home Destinations:** Designate one Home translocator per network.
- **Travel Modes:** Right-click to toggle between Random and Go Home.
- **Visual Indicators:** Green particles for Random, red for Go Home, and purple for designated Home.
- **Network Dropdown:** Select existing networks or enter names manually.
- **Network-Wide Renaming:** Rename an entire network, including translocators in unloaded chunks.
- **Safety Confirmations:** Prevent accidental Home replacement and conflicting network names.
- **Persistent Settings:** Home designations and travel modes survive saving and reloading.

## Controls

| Action | Control |
|---|---|
| Toggle Random / Go Home | Right-click |
| Open Translocator Configurator | Shift + Right-click |
| Rename entire network | Shift + Right-click the designated Home translocator |

**Important:** Network-wide renaming is available only from the designated Home translocator.

## Installation and Upgrading

**Back up your world before installing or updating any mod.**

1. Remove or disable your previous Translocator Relocator installation.
2. Install Translocator Relocator +Configurator.
3. Load your world and verify your existing translocators.

The enhanced mod retains the `translocatorrelocatorfork` block asset identifiers to preserve compatibility with existing worlds.

Upgrading has been successfully tested with Tirello's fork and older saved worlds. Existing translocators, network information, and updated settings survived save-and-reload testing.

**Do not enable multiple versions of Translocator Relocator simultaneously.**

## Known Issues

Removing or destroying translocators while the mod is disabled may leave outdated entries in the network registry.

The inherited `/tr wipeAllData` command clears all stored network records, not just invalid entries. Use extreme caution.

A safer network-repair feature is being considered for a future release.

## Building from Source

Requirements:

- .NET 10 SDK
- Vintage Story 1.22+ installation

```powershell
$env:VINTAGE_STORY = "$env:APPDATA\Vintagestory"
dotnet build TranslocatorRelocator/TranslocatorRelocator.csproj -c Release
```

## Reporting Issues

Please use the [GitHub Issue Tracker](https://github.com/wesley947/Translocator-Relocator-Configurator/issues) for bug reports, suggestions, and feature requests.

## Credits

- **Windows98:** Original creator of Translocator Relocator. [Original Mod](https://mods.vintagestory.at/show/mod/22557)
- **Tirello:** Vintage Story 1.22 compatibility fork, migration support, and teleportation fixes. [Tirello's Fork](https://mods.vintagestory.at/show/mod/49324)
- **Lingam:** +Configurator enhancements, including Home destinations, travel modes, network management, interface improvements, and visual indicators.

Special thanks to Windows98 and Tirello for making these enhancements possible.

## License

MIT — see [LICENSE](LICENSE). Original copyright notices are retained.