// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Enums;
using System.Collections.Generic;

namespace JuliusSweetland.OptiKey.Models
{
    public class KeyCommand
    {
        public KeyCommand() { }

        // FIXME: resolving merge. Adam's code deprecates the KeyValue member and uses 
        // string to encode KeyValue where appropriate, but this isn't suitable for my changes 
        // as I added a string payload to some key values. For now I'm adding back in the member
        // variable, which will only be used by my code, but this needs to be reviewed.

        public KeyCommand(KeyCommands name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public KeyCommand(KeyCommands name, KeyValue keyValue)
        {
            this.Name = name;
            this.KeyValue = keyValue;
        }

        public KeyCommands Name { get; set; }
        public string Value { get; set; }
        public KeyValue KeyValue { get; set; }
        public bool BackAction { get; set; }
        public string Method { get; set; }
        public List<DynamicArgument> Argument { get; set; }
        public List<KeyCommand> LoopCommands { get; set; }
    }
}
