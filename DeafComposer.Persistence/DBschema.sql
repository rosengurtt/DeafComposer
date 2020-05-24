DROP TABLE IF EXISTS  InstanceNotes
DROP TABLE IF EXISTS  SongSimplificationNotes
DROP TABLE IF EXISTS  Instances
DROP TABLE IF EXISTS  Artifacts
DROP TABLE IF EXISTS  ArtifactTypes
DROP TABLE IF EXISTS  PitchBendItems
DROP TABLE IF EXISTS  Notes
DROP TABLE IF EXISTS  ArtifactTypes
DROP TABLE IF EXISTS  SongSimplifications
DROP TABLE IF EXISTS  TempoChanges
DROP TABLE IF EXISTS  Bars
DROP TABLE IF EXISTS  SongStats
DROP TABLE IF EXISTS  Songs
DROP TABLE IF EXISTS  Bands
DROP TABLE IF EXISTS  Styles
DROP TABLE IF EXISTS  TimeSignatures

CREATE TABLE TimeSignatures(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	Numerator int NOT NULL,
	Denominator int NOT NULL,
)


SET IDENTITY_INSERT TimeSignatures ON

insert into TimeSignatures(Id, Numerator, Denominator)
values (1, 4, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (2, 3, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (3, 2, 2)

insert into TimeSignatures(Id, Numerator, Denominator)
values (4, 6, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (5, 12, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (6, 7, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (7, 2, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (8, 5, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (9, 9, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (10, 3, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (11, 9, 16)

insert into TimeSignatures(Id, Numerator, Denominator)
values (12, 6, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (13, 8, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (14, 12, 16)

insert into TimeSignatures(Id, Numerator, Denominator)
values (15, 3, 2)

insert into TimeSignatures(Id, Numerator, Denominator)
values (16, 6, 16)

insert into TimeSignatures(Id, Numerator, Denominator)
values (17, 4, 2)

insert into TimeSignatures(Id, Numerator, Denominator)
values (18, 4, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (19, 7, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (20, 13, 8)

insert into TimeSignatures(Id, Numerator, Denominator)
values (21, 12, 4)

insert into TimeSignatures(Id, Numerator, Denominator)
values (22, 8, 4)
SET IDENTITY_INSERT TimeSignatures OFF

CREATE TABLE Styles(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	[Name] nvarchar(60) NULL,
)
ALTER TABLE Styles ADD CONSTRAINT UC_Styles UNIQUE (Name);

SET IDENTITY_INSERT Styles ON

insert into Styles(Id, [Name])
values (1, 'Classic')

insert into Styles(Id, [Name])
values (2, 'Rock')

insert into Styles(Id, [Name])
values (3, 'Jazz')

insert into Styles(Id, [Name])
values (4, 'Reggae')

insert into Styles(Id, [Name])
values (5, 'Country')

insert into Styles(Id, [Name])
values (6, 'Soul')

insert into Styles(Id, [Name])
values (7, 'Blues')

insert into Styles(Id, [Name])
values (8, 'Electronic Dance')

insert into Styles(Id, [Name])
values (9, 'World')

insert into Styles(Id, [Name])
values (10, 'Religious')


SET IDENTITY_INSERT Styles OFF

CREATE TABLE Bands(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	[Name] nvarchar(100) NOT NULL,
	StyleId bigint NOT NULL,
)
ALTER TABLE Bands ADD CONSTRAINT UC_Bands UNIQUE (Name);

ALTER TABLE Bands  WITH CHECK ADD  CONSTRAINT FK_Bands_Styles FOREIGN KEY(StyleId)
REFERENCES Styles (Id)
GO

ALTER TABLE Bands CHECK CONSTRAINT FK_Bands_Styles
GO

create table SongStats(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
    SongId bigint not null,
	DurationInSeconds bigint null,
	HasMoreThanOneChannelPerTrack bit NULL,
	HasMoreThanOneInstrumentPerTrack bit NULL,
	HighestPitch bigint null,
	LowestPitch bigint null,
	NumberBars bigint null,
	NumberOfTicks bigint null,
	TempoInBeatsPerMinute bigint null,
	TempoInMicrosecondsPerBeat bigint null,
	TimeSignatureId bigint NULL,
	TotalDifferentPitches bigint null,
	TotalUniquePitches bigint null,
	TotalTracks bigint null,
    TotalTracksWithoutNotes bigint null,
    TotalBassTracks bigint null,
	TotalChordTracks bigint null,
	TotalMelodicTracks bigint null,
    TotalPercussionTracks bigint null,
	TotalInstruments bigint null,
    InstrumentsAsString nvarchar(400)  null,
	TotalPercussionInstruments bigint null,
    TotalChannels bigint null,
	TotalTempoChanges bigint null,
    TotalEvents bigint null,
	TotalNoteEvents bigint null,
	TotalPitchBendEvents bigint null,
	TotalControlChangeEvents bigint null,
    TotalProgramChangeEvents bigint null,
	TotalSustainPedalEvents bigint null,
	TotalChannelIndependentEvents bigint null
)



CREATE TABLE Songs(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	[Name] nvarchar(500) NOT NULL,
	BandId bigint NULL,
	StyleId bigint NOT NULL,
	MidiBase64Encoded nvarchar(max) NOT NULL
)


ALTER TABLE Songs  WITH CHECK ADD  CONSTRAINT FK_Songs_Bands FOREIGN KEY(BandId)
REFERENCES Bands (Id)
GO

ALTER TABLE Songs CHECK CONSTRAINT FK_Songs_Bands
GO

ALTER TABLE Songs  WITH CHECK ADD  CONSTRAINT FK_Songs_Styles FOREIGN KEY(StyleId)
REFERENCES Styles (Id)
GO

ALTER TABLE Songs CHECK CONSTRAINT FK_Songs_Styles
GO

ALTER TABLE SongStats  WITH CHECK ADD  CONSTRAINT FK_SongStats_TimeSignatures FOREIGN KEY(TimeSignatureId)
REFERENCES TimeSignatures (Id)
GO

ALTER TABLE SongStats CHECK CONSTRAINT FK_SongStats_TimeSignatures
GO

CREATE TABLE SongSimplifications(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	SimplificationVersion bigint not null,
	SongId bigint not null,
    NumberOfVoices bigint not null
) 


CREATE TABLE Notes(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	Pitch tinyint NOT NULL,
	Volume tinyint NOT NULL,
	StartSinceBeginningOfSongInTicks bigint NOT NULL,
	EndSinceBeginningOfSongInTicks bigint NOT NULL,
	Instrument tinyint NOT NULL,
	IsPercussion bit null,
	Voice tinyint not null
)

create table SongSimplificationNotes(
    Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
    NoteId bigint,
    SongSimplificationId bigint
    )
ALTER TABLE SongSimplificationNotes  WITH CHECK ADD  CONSTRAINT FK_SongSimplificationNote_Note FOREIGN KEY(NoteId)
REFERENCES Notes (Id)
ALTER TABLE SongSimplificationNotes  WITH CHECK ADD  CONSTRAINT FK_SongSimplificationNote_SongSimplification FOREIGN KEY(SongSimplificationId)
REFERENCES SongSimplifications (Id)



CREATE TABLE Bars(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	BarNumber bigint null,
	TicksFromBeginningOfSong bigint NULL,
	TimeSignatureId bigint NULL,
	HasTriplets bit NULL,
	TempoInMicrosecondsPerQuarterNote bigint null,
	SongId bigint not null
)
ALTER TABLE Bars  WITH CHECK ADD  CONSTRAINT FK_Bars_TimeSignatures FOREIGN KEY(TimeSignatureId)
REFERENCES TimeSignatures (Id)

ALTER TABLE Bars  WITH CHECK ADD  CONSTRAINT FK_Bars_Songs FOREIGN KEY(SongId)
REFERENCES Songs (Id)

CREATE TABLE PitchBendItems(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	TicksSinceBeginningOfSong bigint NULL,
	Pitch int null,
	NoteId bigint not null
) 
ALTER TABLE PitchBendItems  WITH CHECK ADD  CONSTRAINT FK_PitchBendItems_Notes FOREIGN KEY(NoteId)
REFERENCES Notes (Id)



CREATE TABLE TempoChanges(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	TicksSinceBeginningOfSong bigint NULL,
	MicrosecondsPerQuarterNote bigint null,
	SongId bigint not null
) 
ALTER TABLE TempoChanges  WITH CHECK ADD  CONSTRAINT FK_TempoChanges_Songs FOREIGN KEY(SongId)
REFERENCES Songs (Id)

create table ArtifactTypes(
	Id tinyint primary key clustered NOT NULL,
	TypeName varchar(100) not null
)

insert into ArtifactTypes(Id, TypeName)
values (1, 'PitchPattern')
insert into ArtifactTypes(Id, TypeName)
values (2, 'RythmPattern')
insert into ArtifactTypes(Id, TypeName)
values (3, 'MelodyPattern')
insert into ArtifactTypes(Id, TypeName)
values (4, 'Chord')
insert into ArtifactTypes(Id, TypeName)
values (5, 'ChordProgression')
     

CREATE TABLE Artifacts(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	AsString varchar(1000) not null,
	ArtifactTypeId tinyint not null,
	CONSTRAINT IX_UniqueArtifacts UNIQUE (AsString, ArtifactTypeId)
) 
ALTER TABLE Artifacts  WITH CHECK ADD  CONSTRAINT FK_Artifacts_ArtifactTypes FOREIGN KEY(ArtifactTypeId)
REFERENCES ArtifactTypes (Id)

CREATE TABLE Instances(
	Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
	SongSimplificationId bigint not null,
	ArtifactId bigint not null
) 
ALTER TABLE Instances  WITH CHECK ADD  CONSTRAINT FK_Instances_SongSimplifications FOREIGN KEY(SongSimplificationId)
REFERENCES SongSimplifications (Id)
ALTER TABLE Instances  WITH CHECK ADD  CONSTRAINT FK_Instances_Artifacts FOREIGN KEY(ArtifactId)
REFERENCES Artifacts (Id)


CREATE TABLE InstanceNotes(
    Id bigint IDENTITY(1,1) primary key clustered NOT NULL,
    InstanceId bigint not null,
    NoteId bigint not null
)
ALTER TABLE InstanceNotes  WITH CHECK ADD  CONSTRAINT FK_InstanceNotes_Instances FOREIGN KEY(InstanceId)
REFERENCES Instances (Id)
ALTER TABLE InstanceNotes  WITH CHECK ADD  CONSTRAINT FK_InstanceNotes_Notes FOREIGN KEY(NoteId)
REFERENCES Notes (Id)

