namespace Temple.Domain.Identity;

public static class RoleCapabilities
{
    public const string TenantOwner = "tenant_owner";
    public const string Leader = "leader";
    public const string Contributor = "contributor";
    public const string Member = "member";
    public const string Guest = "guest";

    private static readonly Dictionary<string,string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [TenantOwner] = Capability.All,
        [Leader] = new[]
        {
            Capability.OrgManageSettings,
            Capability.OrgViewAudit,
            Capability.ScheduleCreateEvent,
            Capability.ScheduleManageEvent,
            Capability.ScheduleRead,
            Capability.ChatPostMessage,
            Capability.ChatManageChannel,
            Capability.ChatCreateChannel,
            Capability.ChatDeleteChannel,
            Capability.ChatReadPresence,
            Capability.ChatPostAnnouncement,
            Capability.DonationViewSummary,
            Capability.DonationCreate,
            Capability.AutomationManageRules,
            Capability.NotificationSend,
            Capability.ProfileUpdate,
            Capability.ContentCreateLesson,
            Capability.ContentPublishLesson,
            Capability.MediaUpload,
            Capability.PeopleRecordAttendance,
            Capability.PeopleAddCareNote,
            Capability.PeopleAddPrayerRequest,
            Capability.PeopleAddMilestone,
            Capability.GroupsRead,
            Capability.GroupsManage,
            Capability.GroupsJoin,
            Capability.GroupsRecordAttendance,
            Capability.GroupsManageCurriculum,
            Capability.GroupsManagePathway
            ,Capability.SongsManage
            ,Capability.SongsRead
            ,Capability.ServicePlansManage
            ,Capability.ServicePlansRead
            ,Capability.SetListsPublish
            ,Capability.VolunteersManagePositions
            ,Capability.VolunteersAssign
            ,Capability.VolunteersRecordAvailability
            ,Capability.VolunteersBackgroundCheck
            ,Capability.ChildrenCheckIn
            ,Capability.ChildrenIncidentReport
            ,Capability.StewardshipManageCampaigns
            ,Capability.StewardshipManageFunds
            ,Capability.StewardshipRecordNonCash
            ,Capability.FinanceManageBudget
            ,Capability.FinanceSubmitExpense
            ,Capability.FinanceApproveExpense
            ,Capability.FacilitiesBook
            ,Capability.FacilitiesManageBookings
        },
        [Contributor] = new[]
        {
            Capability.ScheduleCreateEvent,
            Capability.ScheduleRead,
            Capability.ChatPostMessage,
            Capability.ChatManageChannel,
            Capability.ChatCreateChannel,
            Capability.ChatReadPresence,
            Capability.DonationCreate,
            Capability.ProfileUpdate,
            Capability.ContentCreateLesson,
            Capability.MediaUpload,
            Capability.PeopleRecordAttendance,
            Capability.PeopleAddPrayerRequest,
            Capability.PeopleAddMilestone,
            Capability.GroupsRead,
            Capability.GroupsManage,
            Capability.GroupsJoin,
            Capability.GroupsRecordAttendance,
            Capability.GroupsManageCurriculum
            ,Capability.SongsManage
            ,Capability.SongsRead
            ,Capability.ServicePlansManage
            ,Capability.ServicePlansRead
            ,Capability.VolunteersManagePositions
            ,Capability.VolunteersAssign
            ,Capability.VolunteersRecordAvailability
            ,Capability.ChildrenCheckIn
            ,Capability.ChildrenIncidentReport
            ,Capability.StewardshipManageCampaigns
            ,Capability.StewardshipManageFunds
            ,Capability.StewardshipRecordNonCash
            ,Capability.FinanceSubmitExpense
            ,Capability.FacilitiesBook
        },
        [Member] = new[]
        {
            Capability.ScheduleRead,
            Capability.ChatPostMessage,
            Capability.ChatReadPresence,
            Capability.DonationCreate,
            Capability.ProfileUpdate
            ,Capability.PeopleAddPrayerRequest
            ,Capability.GroupsRead
            ,Capability.GroupsJoin
            ,Capability.SongsRead
            ,Capability.ServicePlansRead
            ,Capability.VolunteersRecordAvailability
            ,Capability.ChildrenCheckIn
            ,Capability.FinanceSubmitExpense
            ,Capability.FacilitiesBook
        },
        [Guest] = new[]
        {
            Capability.ScheduleRead
        }
    };

    public static IReadOnlyCollection<string> Get(string roleKey) =>
        Map.TryGetValue(roleKey, out var caps) ? caps : Array.Empty<string>();
}
