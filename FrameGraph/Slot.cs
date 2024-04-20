using System.Collections.Generic;
using FrameGraph.Node;
using UnityEngine;

namespace FrameGraph
{
    public enum SlotType
    {
        Input,
        Output
    }

    public class Slot
    {
        public string PortName;
        public SlotType SlotType;
        public NodeBase ParentNode;

        public List<Slot> Output = new();
        public Slot Input;
    }

    public class Slot<T> : Slot
    {
        public T Value;

        public void Bind(T value)
        {
            Value = value;
        }

        public T InputValue
        {
            get
            {
                if (Input is Slot<T> prev)
                {
                    return prev.Value;
                }
                else if (Input is StaticSlot<T> prevStatic)
                {
                    return prevStatic.Value;
                }
                else
                {
                    return default;
                }
            }
        }
    }

    public class StaticSlot<T> : Slot
    {
        public string Key;

        public T Value
        {
            get
            {
                if (string.IsNullOrEmpty(Key))
                {
                    Debug.LogWarning("Key is null or empty, may not get correct value");
                }

                return Blackboard<T>.Get(Key);
            }
        }

        public T InputValue
        {
            get
            {
                if (Input is StaticSlot<T> prevStatic)
                {
                    return prevStatic.Value;
                }
                else if (Input is Slot<T> prev)
                {
                    return prev.Value;
                }
                else
                {
                    return default;
                }
            }
        }
    }
}