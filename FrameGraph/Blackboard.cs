using System;
using System.Collections.Generic;

namespace FrameGraph
{
    public class Blackboard<T>
    {
        public static Dictionary<string, T> BlackBoardItems = new();
        public static T                     SingleItem      = default;
        public static bool                  SingleItemValid = false;

        public static T Get()
        {
            if (!SingleItemValid)
            {
                throw new NullReferenceException("Please set first before get");
            }
            return SingleItem;
        }
        
        public static bool TryGet(out T value)
        {
            value = SingleItem;
            return SingleItemValid;
        }
        
        public static T Get(string key)
        {
            if (BlackBoardItems.TryGetValue(key, out var result))
            {
                return result;
            }
            else
            {
                throw new NullReferenceException($"Key not found:{key}");
            }
        }

        public static bool TryGet(string key, out T value)
        {
            return BlackBoardItems.TryGetValue(key, out value);
        }
        
        public static void Set(T value)
        {
            SingleItem = value;
            SingleItemValid = true;
        }
        
        public static void Set(string key, T value)
        {
            BlackBoardItems[key] = value;
        }

        public static bool Clear()
        {
            bool lastState = SingleItemValid;
            SingleItem      = default;
            SingleItemValid = false;
            return lastState;
        }
        
        public static bool Clear(string key)
        {
            return BlackBoardItems.Remove(key);
        }
        
        public static bool IsValid()
        {
            return SingleItemValid;
        }

        public static bool IsValid(string key)
        {
           return BlackBoardItems.ContainsKey(key); 
        }
    }
}