namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// Used for a join table between Notes and Instances
    /// </summary>
    public class InstanceNote
    {

            public long Id { get; set; }
            public long InstanceId { get; set; }
            public long NoteId { get; set; }
        }
    }

