namespace ShnorSignature
{
    public enum StepType
    {
        NONE = 0,
        GENERATE_COMMON_PARAMETERS = 1,
        GENERATE_PRIVATE_KEY = 2,
        GENERATE_PUBLIC_KEY = 3,
        CREATE_CALC_k = 4,
        CREATE_CALC_R = 5,
        CREATE_CALC_e = 6,
        CREATE_CALC_s = 7,
        VERIFY_CALC_R = 8,
        VERIFY_CALC_e = 9,
        VERIFY_SIGN = 10
    }
}
