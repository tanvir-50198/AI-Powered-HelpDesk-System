namespace HelpDeskPro.Models
{
    public enum TicketStatus
    {
        Open = 1,
        InProgress = 2,
        Resolved = 3,
        Closed = 4
    }

    public enum TicketPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    // Is the document allowed to be used by the chatbot?
    public enum ApprovalStatus
    {
        Draft = 1,
        Approved = 2,
        Rejected = 3
    }

    // Did text extraction / chunking / embedding finish?
    public enum ProcessingStatus
    {
        Pending = 1,
        Indexed = 2,
        Failed = 3
    }

    // Who may read this document through the chatbot?
    public enum DocumentAudience
    {
        Customer = 1,   // customers, agents and admins
        Agent = 2,      // agents and admins only
        Internal = 3    // admins only
    }
}
