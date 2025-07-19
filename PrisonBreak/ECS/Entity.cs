using System;
using System.Collections.Generic;

namespace PrisonBreak.ECS;

public class Entity
{
    public int Id { get; }
    private Dictionary<Type, object> _components = new();
    private Action<int, Type> _onComponentAdded;
    private Action<int, Type> _onComponentRemoved;
    
    public Entity(int id)
    {
        Id = id;
    }
    
    internal void SetComponentCallbacks(Action<int, Type> onAdded, Action<int, Type> onRemoved)
    {
        _onComponentAdded = onAdded;
        _onComponentRemoved = onRemoved;
    }
    
    public T AddComponent<T>(T component)
    {
        var componentType = typeof(T);
        _components[componentType] = component;
        _onComponentAdded?.Invoke(Id, componentType);
        return component;
    }
    
    public ref T GetComponent<T>() where T : struct
    {
        if (_components.TryGetValue(typeof(T), out var component))
        {
            return ref System.Runtime.CompilerServices.Unsafe.Unbox<T>(component);
        }
        throw new InvalidOperationException($"Component {typeof(T).Name} not found on entity {Id}");
    }
    
    public bool HasComponent<T>()
    {
        return _components.ContainsKey(typeof(T));
    }
    
    public bool TryGetComponent<T>(out T component)
    {
        if (_components.TryGetValue(typeof(T), out var comp))
        {
            component = (T)comp;
            return true;
        }
        component = default(T);
        return false;
    }
    
    public void RemoveComponent<T>()
    {
        var componentType = typeof(T);
        if (_components.Remove(componentType))
        {
            _onComponentRemoved?.Invoke(Id, componentType);
        }
    }
    
    public IEnumerable<Type> GetComponentTypes()
    {
        return _components.Keys;
    }
}