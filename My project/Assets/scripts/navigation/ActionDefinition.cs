using System;
using System.Collections.Generic;

namespace Assets.logic
{
    internal sealed class ActionDefinition
    {
        internal ActionDefinition(string command, IReadOnlyList<string> parameters)
        {
            Command = command;
            Parameters = parameters ?? Array.Empty<string>();
        }

        internal string Command { get; }
        internal IReadOnlyList<string> Parameters { get; }
    }
}
