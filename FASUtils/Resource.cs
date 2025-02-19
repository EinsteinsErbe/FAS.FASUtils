﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FASUtils
{
    public class Resource
    {
        public bool online { get; protected set; }
        public string Name { get; }
        public string Meta { get; set; }
        public int Timeout = 5000;
        protected Func<(bool, string)> checkAction;
        public Task checkTask;
        public Action coninuationAction;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public class StateChangedEventArgs : EventArgs
        {
            public ConnectState State { get; set; }
            public string Message { get; set; }
        }

        protected virtual void OnStateChanged(StateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        public Resource(string Name)
        {
            this.Name = Name;
        }

        public Resource(string Name, Func<(bool, string)> checkAction)
        {
            this.Name = Name;
            SetCheckAction(checkAction);
        }

        protected void SetCheckAction(Func<(bool, string)> checkAction)
        {
            this.checkAction = checkAction;
            checkTask = new Task(() =>
            {
                Logger.Log("Checking " + Name, this);

                (online, Meta) = checkAction();
                ChangeState(online ? ConnectState.CONNECTED : ConnectState.FAILED, (online ? "OK" : "ERROR"));

                coninuationAction?.Invoke();
            });
        }

        public Task Check()
        {
            ChangeState(ConnectState.CONNECTING, "checking...");

            switch (checkTask.Status)
            {
                case TaskStatus.Created: checkTask.Start(); break;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                case TaskStatus.RanToCompletion: SetCheckAction(checkAction); checkTask.Start(); break;
            }
            return checkTask;
        }

        public void ChangeState(ConnectState state, string name)
        {
            OnStateChanged(new StateChangedEventArgs
            {
                State = state,
                Message = Name + Meta + ": " + name
            });
            Logger.Debug(Name + Meta + " Status: " + name, this);
        }
    }

    public enum ConnectState
    {
        CONNECTING, CONNECTED, FAILED, TIMEOUT
    }
}
