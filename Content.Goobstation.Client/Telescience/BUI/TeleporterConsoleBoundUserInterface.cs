// SPDX-License-Identifier: MIT

using Content.Client.Computer;
using Content.Goobstation.Client.Telescience.UI;
using Content.Goobstation.Shared.Telescience.Teleporter.BUIStates;
using JetBrains.Annotations;

namespace Content.Goobstation.Client.Telescience.BUI;

[UsedImplicitly]
public sealed class TeleporterConsoleBoundUserInterface : ComputerBoundUserInterface<TeleporterConsoleWindow, TeleporterConsoleBoundUserInterfaceState>
{
    public TeleporterConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
}