namespace BA_MU.Core.Utils;

public static class TypeMapper
{
    private static readonly Dictionary<int, string> TypeMap = new()
    {
        { 1, "GameObject" },
        { 4, "Transform" },
        { 21, "Material" },
        { 23, "MeshRenderer" },
        { 25, "MeshFilter" },
        { 28, "Texture2D" },
        { 33, "MeshCollider" },
        { 43, "Mesh" },
        { 48, "Shader" },
        { 61, "Animation" },
        { 64, "MeshCollider" },
        { 65, "BoxCollider" },
        { 74, "AnimationClip" },
        { 82, "AudioSource" },
        { 83, "AudioClip" },
        { 108, "Behaviour" },
        { 114, "MonoBehaviour" },
        { 115, "MonoScript" },
        { 128, "Font" },
        { 134, "PhysicMaterial" },
        { 135, "SphereCollider" },
        { 136, "CapsuleCollider" },
        { 137, "SkinnedMeshRenderer" },
        { 138, "FixedJoint" },
        { 141, "BuildSettings" },
        { 142, "AssetBundle" },
        { 143, "CharacterController" },
        { 144, "CharacterJoint" },
        { 145, "SpringJoint" },
        { 146, "WheelCollider" },
        { 147, "ResourceManager" },
        { 148, "NetworkView" },
        { 149, "NetworkManager" },
        { 150, "EllipsoidParticleEmitter" },
        { 151, "ParticleAnimator" },
        { 152, "ParticleRenderer" },
        { 153, "ParticleSystem" },
        { 154, "ParticleSystemRenderer" }
    };

    public static string GetAssetTypeName(int typeId) => TypeMap.TryGetValue(typeId, out var name) ? name : $"Unknown_{typeId}";

    public static IEnumerable<string> GetAllTypes() => TypeMap.Values.OrderBy(x => x);
}