using Microsoft.Win32;

namespace IGameInstaller.Helper
{
    public static class RegistryHelper
    {
        public static object GetResourceRegistry(string resourceId, string key)
        {
            using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey(App.ProjectName);
            if (registryKey == null) return null;
            using RegistryKey registryKey2 = registryKey.OpenSubKey(resourceId);
            if (registryKey2 == null) return null;
            return registryKey2.GetValue(key, null);
        }
        public static void SetResourceRegistry(string resourceId, string key, object value)
        {
            using RegistryKey registryKey = Registry.LocalMachine.CreateSubKey("SOFTWARE").CreateSubKey(App.ProjectName).CreateSubKey(resourceId);
            registryKey.SetValue(key, value);
        }
        public static void RemoveResourceRegistry(string resourceId)
        {
            using RegistryKey registryKey = Registry.LocalMachine.CreateSubKey("SOFTWARE").OpenSubKey(App.ProjectName, true);
            if (registryKey != null)
            {
                registryKey.DeleteSubKey(resourceId);
            }
        }
        public enum RegisterType
        {
            localMachine,
            currentUser,
            classesRoot,
        }
        public static object GetRegistry(string path, string key, RegisterType type = RegisterType.localMachine)
        {
            RegistryKey registryKey;
            if (type == RegisterType.localMachine)
            {
                registryKey = Registry.LocalMachine;
            } 
            else if (type == RegisterType.currentUser)
            {
                registryKey = Registry.CurrentUser;
            }
            else
            {
                registryKey = Registry.ClassesRoot;
            }
            using RegistryKey registryKey2 = registryKey.OpenSubKey(path);
            if (registryKey2 == null) return null;
            return registryKey2.GetValue(key, null);
        }
        public static void SetRegistry(string path, string key, object value, RegisterType type = RegisterType.localMachine)
        {
            RegistryKey registryKey;
            if (type == RegisterType.localMachine)
            {
                registryKey = Registry.LocalMachine;
            }
            else if (type == RegisterType.currentUser)
            {
                registryKey = Registry.CurrentUser;
            }
            else
            {
                registryKey = Registry.ClassesRoot;
            }
            using RegistryKey registryKey2 = registryKey.CreateSubKey(path);
            if (registryKey2 == null) return;
            registryKey2.SetValue(key, value);
        }
    }
}
