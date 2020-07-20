// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Diagnostics;

/**
 * This is a plugin that runs a command in windows. It can be anything that you would run using command line prompt.
 * 
 * It has two methods
 * 
 * 1) RUN
 *    . command: the command that will be executed by the plugin
 * 2) RUN_ARGS
 *    . command: the command that will be executed by the plugin
 *    . arguments: the arguments to pass
 * 
 * Please refer to OptiKey wiki for more information on registering and developing extensions.
 */

namespace JuliusSweetland.OptiKey.StandardPlugins
{
    public class ExternalProgram
    {
        // Simply run it.
        public void RUN(string command)
        {
            Process.Start(command);
        }

        // Run with arguments
        public void RUN_ARGS(string command, string arguments)
        {
            Process.Start(command, arguments);
        }
    }
}
