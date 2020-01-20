using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance = null;
    public static T Instance => instance;

    public virtual void Awake()
    {
        instance = (T)FindObjectOfType(typeof(T));
    }
}

public class Singleton<T> where T : class, new()
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance== null)
            {
                instance = new T();
            }
            return instance;
        }
    }

    protected Singleton() { }
}
