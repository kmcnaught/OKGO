using JuliusSweetland.OptiKey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    class KeyPauseHandler
    {
        
        private Action pauseAction;
        private Action resumeAction;
        private bool paused = false;
        Func<Point, bool> WhenRequiresPausing;

        public EventHandler<Tuple<Point, KeyValue>> inputServiceCurrentPositionHandler;

        public KeyPauseHandler(Func<Point, bool> whenRequiresPausing,
                               Action pauseAction,
                               Action resumeAction)
        {
            this.WhenRequiresPausing = whenRequiresPausing;
            this.pauseAction = pauseAction;
            this.resumeAction = resumeAction;

            // Set up position callback
            inputServiceCurrentPositionHandler = (o, tuple) =>
            {
                this.UpdateFromPosition(tuple.Item1, tuple.Item2);
            };

        }

        private void UpdateFromPosition(Point point, KeyValue keyValue)
        {
            bool needsPausing = WhenRequiresPausing(point);
            if (needsPausing && !paused)
                this.Pause();
            else if (!needsPausing && paused)
                this.Resume();
        }

        private void Pause()
        {
            this.paused = true;
            this.pauseAction();
        }

        private void Resume()
        {
            this.paused = false;
            this.resumeAction();
        }

        public void AttachListener(Services.IInputService inputService)
        {
            // Remove first, to ensure only one instance
            inputService.CurrentPosition -= inputServiceCurrentPositionHandler;
            inputService.CurrentPosition += inputServiceCurrentPositionHandler;
        }

        public void DetachListener(Services.IInputService inputService)
        {            
            inputService.CurrentPosition -= inputServiceCurrentPositionHandler;            
        }
    }
}
