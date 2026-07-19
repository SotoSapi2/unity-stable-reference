using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityStableReference;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly)]
public sealed class StableWrapperCodeGenAttribute : Attribute
{ }

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IStableWrapper { }

public interface IStableWrapper<out T> : IStableWrapper
{
    T Value { get; }
}

[Serializable]
public class StableWrapper<T> : IStableWrapper<T> where T : new()
{
#if UNITY_5_6_OR_NEWER
    [UnityEngine.SerializeField]
#endif
    private T value = new();

    public T Value => value;
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates")]
[Serializable]
public struct StableReference<T> where T : class
{
    [UnityEngine.SerializeReference]
    private IStableWrapper? wrapper;

    public T Value => wrapper is not null ? 
        ((IStableWrapper<T>)wrapper).Value : 
        throw new NullReferenceException("Accessed StableReference instance doesn't have value assigned.");
    
    public bool IsValuePresent => wrapper is not null;

    public static implicit operator T(StableReference<T> self) => self.Value;
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates")]
[Serializable]
public struct NullableStableReference<T> where T : class?
{
    [UnityEngine.SerializeReference]
    private IStableWrapper? wrapper;

    public T? Value => wrapper is not null ? 
        ((IStableWrapper<T>)wrapper).Value : 
        null;

    public static implicit operator T?(NullableStableReference<T> self) => self.Value;
}
