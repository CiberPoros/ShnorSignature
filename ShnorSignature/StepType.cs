namespace ShnorSignature
{
    public enum StepType
    {
        NONE = 0,
        GENERATE_COMMON_PARAMETERS = 1,
        GENERATE_PRIVATE_KEY = 2,
        GENERATE_PUBLIC_KEY = 3,
        CREATE_SIGNATURE = 4,
        VERIFY_SIGNATURE = 5
    }
}
