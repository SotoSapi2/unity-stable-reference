using UnityEditor;

namespace UnityStableReference.Editor
{
    [CustomPropertyDrawer(typeof(NullableStableReference<>))]
    public sealed class NullableStableReferenceDrawer : AbstractStableReferenceDrawer
    { }
}