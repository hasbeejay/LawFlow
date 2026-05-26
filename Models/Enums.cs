namespace LawFlow.Models
{
    public enum UserRole
    {
        Admin,
        Judge,
        Lawyer,
        Client,
        Police,
        Clerk
    }

    public enum CaseStatus
    {
        Created,
        InProgress,
        ReviewedByAdmin,
        AssignedToJudgeAndPolice,
        AvailableForLawyers,
        LawyerAccepted,
        AssignedToLawyer,
        ClerkAssignedByJudge,
        Investigation,
        Hearing,
        VerdictIssued,
        Closed
    }

    public enum VerdictType
    {
        Guilty,
        Acquitted,
        Dismissed,
        Appealed
    }

    public enum ChatChannel
    {
        ClientLawyer = 0,
        JudgeClerk = 1,
        AdminPolice = 2
    }
}
