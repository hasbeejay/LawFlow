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
        Created,                  // 0: FIR Lodged by Client
        ReviewedByAdmin,          // 1: Admin reviews and approves complaint
        AssignedToJudgeAndPolice, // 2: Admin assigns Judge & Police investigator
        AvailableForLawyers,      // 3: Opened for legal defense acceptance
        LawyerAccepted,           // 4: Lawyer has selected/clicked accept
        AssignedToLawyer,         // 5: Admin or system finalizes lawyer assignment
        ClerkAssignedByJudge,     // 6: Presiding Judge assigns Clerk to case
        Investigation,            // 7: Police investigator uploads reports
        Hearing,                  // 8: Clerk schedules & holds court hearings
        VerdictIssued,            // 9: Judge files official verdict
        Closed                    // 10: Clerk publishes verdict, archiving case
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
