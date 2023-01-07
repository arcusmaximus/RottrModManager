namespace RottrModManager.Shared.Cdc
{
    public enum ResourceType
    {
        Unknown = 0,
        Animation = 2,
        Unused = 3,
        PsdRes = 4,
        Texture = 5,
        Sound = 6,
        Dtp = 7,
        Script = 8,
        Shader = 9,
        Material = 10,
        Object = 11,
        Mesh = 12,
        CollisionMesh = 13,
        GroupList = 14,
        Trigger = 15
    }

    public enum Locale
    {
        En = 1,
        Fr = 2,
        De = 4,
        It = 8,
        Es419 = 0x10,
        Es = 0x20,
        Ja = 0x40,
        Pt = 0x80,
        Pl = 0x100,
        Ru = 0x200,
        Nl = 0x400,
        Ko = 0x800,
        ZhHant = 0x1000,
        ZhHans = 0x2000,
        Ar = 0x8000
    }
}
