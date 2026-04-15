using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace USerialization.Unity
{
    public unsafe class UnityRuntimeUtils : IRuntimeUtils
    {
#if UNITY_EDITOR || ENABLE_MONO
        [DllImport("mono-2.0-bdwgc", EntryPoint = "mono_field_get_offset")]
        private static extern int mono_field_get_offset(IntPtr classField);

        [DllImport("mono-2.0-bdwgc", EntryPoint = "mono_class_instance_size")]
        private static extern int mono_class_instance_size(IntPtr klass);

        [DllImport("mono-2.0-bdwgc", EntryPoint = "mono_class_from_mono_type")]
        private static extern IntPtr mono_class_from_mono_type(IntPtr monoType);

        [DllImport("mono-2.0-bdwgc", EntryPoint = "mono_class_value_size")]
        private static extern int mono_class_value_size(IntPtr monoType, uint* align);
#else
        [DllImport("__Internal", EntryPoint = "mono_field_get_offset")]
        private static extern int mono_field_get_offset(IntPtr classField);

        [DllImport("__Internal", EntryPoint = "mono_class_instance_size")]
        private static extern int mono_class_instance_size(IntPtr klass);

        [DllImport("__Internal", EntryPoint = "mono_class_from_mono_type")]
        private static extern IntPtr mono_class_from_mono_type(IntPtr monoType);

        [DllImport("__Internal", EntryPoint = "mono_class_value_size")]
        private static extern int mono_class_value_size(IntPtr monoType, uint* align);
#endif


        public int GetFieldOffset(FieldInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException(nameof(fi));

            return mono_field_get_offset(fi.FieldHandle.Value) - HeaderSize;
        }

        public int GetStackSize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.IsValueType)
            {
                var monoType = type.TypeHandle;
                var monoClass = mono_class_from_mono_type(monoType.Value);
                var size = mono_class_value_size(monoClass, null);
                return size;
            }

            return sizeof(void*);
        }

        private static int HeaderSize => sizeof(void*) * 2;

        public int GetClassHeapSize(Type type)
        {
            if (type.IsClass == false)
                throw new InvalidOperationException();
            if (type.IsInterface)
                throw new InvalidOperationException();
            if (type.IsAbstract)
                throw new InvalidOperationException();

            var monoType = type.TypeHandle;
            var monoClass = mono_class_from_mono_type(monoType.Value);
            return mono_class_instance_size(monoClass) - HeaderSize;
        }
    }
}