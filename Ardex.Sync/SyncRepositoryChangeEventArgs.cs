using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    public delegate void SyncRepositoryChangeEventHandler<TEntity>(object sender, SyncRepositoryChangeEventArgs<TEntity> e);

    public class SyncRepositoryChangeEventArgs<TEntity> : EventArgs
    {
        public TEntity Entity { get; private set; }
        public SyncEntityChangeAction ChangeAction { get; private set; }
        public SyncRepositoryChangeMode ChangeMode { get; private set; }

        public SyncRepositoryChangeEventArgs(TEntity entity, SyncEntityChangeAction changeAction, SyncRepositoryChangeMode changeMode)
        {
            this.Entity = entity;
            this.ChangeAction = changeAction;
            this.ChangeMode = changeMode;
        }
    }
}
