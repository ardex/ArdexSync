using System;

using Ardex.Reflection;

namespace Ardex.TestClient
{
    public class Dummy : IEquatable<Dummy>
    {
        public int DummyID { get; set; }
        public string Text { get; set; }
        public Guid EntityGuid { get; set; }

        private int HiddenProp { get; set; }
        public static int SharedProp { get; set; }

        public override string ToString()
        {
            return new TypeMapping<Dummy>().ToString(this);
        }

        public Dummy Clone()
        {
            return new Dummy
            {
                EntityGuid = this.EntityGuid,
                Text = this.Text
            };
        }

        public bool Equals(Dummy other)
        {
            if (other == null) return false;

            return new TypeMapping<Dummy>().Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as Dummy);
        }

        public override int GetHashCode()
        {
            return this.EntityGuid.GetHashCode();
        }
    }
}
