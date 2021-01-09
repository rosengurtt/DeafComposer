using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeafComposer.Persistence
{
    public class DBContext: DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }

        public DbSet<Style> Styles { get; set; }
        public DbSet<Band> Bands { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<TimeSignature> TimeSignatures { get; set; }
        public DbSet<KeySignature> KeySignatures { get; set; }

        public DbSet<Note> Notes { get; set; }
        public DbSet<Bar> Bars { get; set; }
        public DbSet<PitchBendItem> PitchBendItems { get; set; }

        public DbSet<TempoChange> TempoChanges { get; set; }

        public DbSet<SongSimplification> SongSimplifications { get; set; }
        public DbSet<SongSimplificationNote> SongSimplificationNotes { get; set; }

        public DbSet<Artifact> Artifacts { get; set; }
        public DbSet<Instance> Instances { get; set; }


        public DbSet<InstanceNote> InstanceNotes { get; set; }
        public DbSet<SongAnalysis> SongAnalysis { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Style>().ToTable("Styles");
            modelBuilder.Entity<Band>().ToTable("Bands");
            modelBuilder.Entity<Song>().ToTable("Songs");
            modelBuilder.Entity<TimeSignature>().ToTable("TimeSignatures");
            modelBuilder.Entity<KeySignature>().ToTable("KeySignatures");
            modelBuilder.Entity<Note>().ToTable("Notes");
            modelBuilder.Entity<Bar>().ToTable("Bars");
            modelBuilder.Entity<PitchBendItem>().ToTable("PitchBendItems");
            modelBuilder.Entity<TempoChange>().ToTable("TempoChanges");
            modelBuilder.Entity<SongSimplification>().ToTable("SongSimplifications");
            modelBuilder.Entity<SongSimplificationNote>().ToTable("SongSimplificationNotes");
            modelBuilder.Entity<Artifact>().ToTable("Artifacts");
            modelBuilder.Entity<Instance>().ToTable("Instances");
            modelBuilder.Entity<InstanceNote>().ToTable("InstanceNotes");
            modelBuilder.Entity<SongAnalysis>().ToTable("SongAnalysis");

            modelBuilder.Entity<Style>()
                .HasAlternateKey(c => c.Name).HasName("IX_StyleName");
            modelBuilder.Entity<Band>()
                .HasAlternateKey(c => c.Name).HasName("IX_BandName");


        }

    }


}
