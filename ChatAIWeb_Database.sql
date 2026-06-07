/* =========================================================
   ChatAIWeb Database Script
   Project: ASP.NET Core MVC + 3 Layers + RAG Chatbot
   Database: SQL Server
   Author: Generated for ChatAIWeb solution
   ========================================================= */

IF DB_ID(N'ChatAIWebDb') IS NULL
BEGIN
    CREATE DATABASE ChatAIWebDb;
END
GO

USE ChatAIWebDb;
GO

/* =========================================================
   Drop tables if needed - keep safe order by FK dependency
   Uncomment this block if you want to recreate database objects.
   ========================================================= */
/*
DROP TABLE IF EXISTS dbo.RagasBenchmarkResults;
DROP TABLE IF EXISTS dbo.EvaluationQuestions;
DROP TABLE IF EXISTS dbo.Citations;
DROP TABLE IF EXISTS dbo.ChatMessages;
DROP TABLE IF EXISTS dbo.ChatSessions;
DROP TABLE IF EXISTS dbo.DocumentConflictFindings;
DROP TABLE IF EXISTS dbo.DocumentConflictCandidates;
DROP TABLE IF EXISTS dbo.DocumentConflictReviews;
DROP TABLE IF EXISTS dbo.DocumentChunkEmbeddings;
DROP TABLE IF EXISTS dbo.DocumentChunks;
DROP TABLE IF EXISTS dbo.Documents;
DROP TABLE IF EXISTS dbo.SubjectEnrollments;
DROP TABLE IF EXISTS dbo.Subjects;
DROP TABLE IF EXISTS dbo.Users;
*/
GO

/* =========================================================
   1. Users
   Roles: Admin / Teacher / Student
   ========================================================= */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        FullName        NVARCHAR(150) NOT NULL,
        Email           NVARCHAR(256) NOT NULL,
        PasswordHash    NVARCHAR(500) NOT NULL,
        Role            NVARCHAR(30)  NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,

        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Teacher', N'Student'))
    );
END
GO

/* =========================================================
   2. Subjects
   Stores courses/subjects whose learning materials are indexed.
   ========================================================= */
IF OBJECT_ID(N'dbo.Subjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subjects
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        SubjectCode     NVARCHAR(50) NOT NULL,
        SubjectName     NVARCHAR(200) NOT NULL,
        Description     NVARCHAR(MAX) NULL,
        CreatedBy       INT NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Subjects_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(0) NULL,

        CONSTRAINT UQ_Subjects_SubjectCode UNIQUE (SubjectCode),
        CONSTRAINT FK_Subjects_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id)
    );
END
GO

/* =========================================================
   3. SubjectEnrollments
   Optional: maps students/teachers to subjects.
   ========================================================= */
IF OBJECT_ID(N'dbo.SubjectEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubjectEnrollments
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        SubjectId   INT NOT NULL,
        UserId      INT NOT NULL,
        RoleInClass NVARCHAR(30) NOT NULL,
        CreatedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_SubjectEnrollments_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_SubjectEnrollments_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id),
        CONSTRAINT FK_SubjectEnrollments_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_SubjectEnrollments_Subject_User UNIQUE (SubjectId, UserId),
        CONSTRAINT CK_SubjectEnrollments_RoleInClass CHECK (RoleInClass IN (N'Teacher', N'Student'))
    );
END
GO

/* =========================================================
   4. Documents
   Stores uploaded PDF/DOCX/PPTX files and indexing status.
   ========================================================= */
IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        SubjectId       INT NOT NULL,
        Title           NVARCHAR(255) NOT NULL,
        OriginalFileName NVARCHAR(255) NOT NULL,
        StoredFileName  NVARCHAR(255) NOT NULL,
        FilePath        NVARCHAR(1000) NOT NULL,
        FileType        NVARCHAR(20) NOT NULL,
        FileSizeBytes   BIGINT NULL,
        UploadedBy      INT NULL,
        Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Documents_Status DEFAULT N'Uploaded',
        ErrorMessage    NVARCHAR(MAX) NULL,
        UploadedAt      DATETIME2(0) NOT NULL CONSTRAINT DF_Documents_UploadedAt DEFAULT SYSUTCDATETIME(),
        IndexedAt       DATETIME2(0) NULL,

        CONSTRAINT FK_Documents_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id),
        CONSTRAINT FK_Documents_Users_UploadedBy FOREIGN KEY (UploadedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Documents_FileType CHECK (FileType IN (N'PDF', N'DOCX', N'PPTX', N'TXT')),
        CONSTRAINT CK_Documents_Status CHECK (Status IN (N'Uploaded', N'Processing', N'Indexed', N'Failed', N'Rejected', N'NeedsReview', N'Deleted'))
    );
END
GO

IF OBJECT_ID(N'dbo.CK_Documents_Status', N'C') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Documents DROP CONSTRAINT CK_Documents_Status;
END
GO

IF OBJECT_ID(N'dbo.CK_Documents_Status', N'C') IS NULL
BEGIN
    ALTER TABLE dbo.Documents
    ADD CONSTRAINT CK_Documents_Status CHECK (Status IN (N'Uploaded', N'Processing', N'Indexed', N'Failed', N'Rejected', N'NeedsReview', N'Deleted'));
END
GO

/* =========================================================
   5. DocumentChunks
   Stores chunked text. Embedding can be stored in VectorDb by VectorId.
   EmbeddingJson is optional for demo if you want to keep vectors in SQL.
   ========================================================= */
IF OBJECT_ID(N'dbo.DocumentChunks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentChunks
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        DocumentId      INT NOT NULL,
        ChunkIndex      INT NOT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        PageNumber      INT NULL,
        SlideNumber     INT NULL,
        TokenCount      INT NULL,
        VectorId        NVARCHAR(100) NULL,
        EmbeddingModel  NVARCHAR(100) NULL,
        EmbeddingJson   NVARCHAR(MAX) NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentChunks_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_DocumentChunks_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.Documents(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_DocumentChunks_Document_ChunkIndex UNIQUE (DocumentId, ChunkIndex)
    );
END
GO

/* =========================================================
   6. DocumentChunkEmbeddings
   Stores one embedding row per chunk and embedding model.
   ========================================================= */
IF OBJECT_ID(N'dbo.DocumentChunkEmbeddings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentChunkEmbeddings
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        DocumentChunkId     INT NOT NULL,
        EmbeddingModel      NVARCHAR(100) NOT NULL,
        EmbeddingProvider   NVARCHAR(50) NOT NULL,
        Dimension           INT NOT NULL,
        VectorId            NVARCHAR(100) NULL,
        VectorStore         NVARCHAR(50) NULL,
        EmbeddingJson       NVARCHAR(MAX) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentChunkEmbeddings_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_DocumentChunkEmbeddings_DocumentChunks FOREIGN KEY (DocumentChunkId) REFERENCES dbo.DocumentChunks(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_DocumentChunkEmbeddings_Chunk_Model UNIQUE (DocumentChunkId, EmbeddingModel)
    );
END
GO

/* =========================================================
   6.1. DocumentConflictReviews
   Stores conflict review sessions for documents that need head-teacher approval.
   ========================================================= */
IF OBJECT_ID(N'dbo.DocumentConflictReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentConflictReviews
    (
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        SubjectId               INT NOT NULL,
        NewDocumentId           INT NOT NULL,
        Status                  NVARCHAR(30) NOT NULL CONSTRAINT DF_DocumentConflictReviews_Status DEFAULT N'Pending',
        Summary                 NVARCHAR(MAX) NOT NULL,
        HighestSimilarityScore  DECIMAL(9,6) NOT NULL,
        FindingCount            INT NOT NULL,
        ResolutionChoice        NVARCHAR(50) NULL,
        ResolvedBy              INT NULL,
        ResolvedAt              DATETIME2(0) NULL,
        ResolutionNote          NVARCHAR(MAX) NULL,
        CreatedAt               DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentConflictReviews_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_DocumentConflictReviews_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id),
        CONSTRAINT FK_DocumentConflictReviews_NewDocument FOREIGN KEY (NewDocumentId) REFERENCES dbo.Documents(Id),
        CONSTRAINT FK_DocumentConflictReviews_ResolvedBy FOREIGN KEY (ResolvedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_DocumentConflictReviews_Status CHECK (Status IN (N'Pending', N'Resolved')),
        CONSTRAINT CK_DocumentConflictReviews_ResolutionChoice CHECK (ResolutionChoice IS NULL OR ResolutionChoice IN (N'AcceptNew', N'KeepExisting', N'NoConflict'))
    );
END
GO

/* =========================================================
   6.2. DocumentConflictCandidates
   Stores indexed documents that were close enough to compare with a new document.
   ========================================================= */
IF OBJECT_ID(N'dbo.DocumentConflictCandidates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentConflictCandidates
    (
        Id                   INT IDENTITY(1,1) PRIMARY KEY,
        ReviewId             INT NOT NULL,
        CandidateDocumentId  INT NOT NULL,
        MaxSimilarityScore   DECIMAL(9,6) NOT NULL,
        FindingCount         INT NOT NULL,
        Summary              NVARCHAR(MAX) NULL,
        CreatedAt            DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentConflictCandidates_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_DocumentConflictCandidates_Reviews FOREIGN KEY (ReviewId) REFERENCES dbo.DocumentConflictReviews(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentConflictCandidates_Documents FOREIGN KEY (CandidateDocumentId) REFERENCES dbo.Documents(Id)
    );
END
GO

/* =========================================================
   6.3. DocumentConflictFindings
   Stores chunk-level differences for audit and teacher review.
   ========================================================= */
IF OBJECT_ID(N'dbo.DocumentConflictFindings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentConflictFindings
    (
        Id                INT IDENTITY(1,1) PRIMARY KEY,
        CandidateId       INT NOT NULL,
        NewChunkId        INT NOT NULL,
        ExistingChunkId   INT NOT NULL,
        SimilarityScore   DECIMAL(9,6) NOT NULL,
        TextSimilarityScore DECIMAL(9,6) NOT NULL CONSTRAINT DF_DocumentConflictFindings_TextSimilarityScore DEFAULT 0,
        Severity          NVARCHAR(30) NOT NULL,
        Explanation       NVARCHAR(MAX) NOT NULL,
        NewSnippet        NVARCHAR(MAX) NOT NULL,
        ExistingSnippet   NVARCHAR(MAX) NOT NULL,
        CreatedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_DocumentConflictFindings_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_DocumentConflictFindings_Candidates FOREIGN KEY (CandidateId) REFERENCES dbo.DocumentConflictCandidates(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentConflictFindings_NewChunk FOREIGN KEY (NewChunkId) REFERENCES dbo.DocumentChunks(Id),
        CONSTRAINT FK_DocumentConflictFindings_ExistingChunk FOREIGN KEY (ExistingChunkId) REFERENCES dbo.DocumentChunks(Id),
        CONSTRAINT CK_DocumentConflictFindings_Severity CHECK (Severity IN (N'Low', N'Medium', N'High'))
    );
END
GO

IF OBJECT_ID(N'dbo.DocumentConflictFindings', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.DocumentConflictFindings', N'TextSimilarityScore') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentConflictFindings
    ADD TextSimilarityScore DECIMAL(9,6) NOT NULL
        CONSTRAINT DF_DocumentConflictFindings_TextSimilarityScore DEFAULT 0;
END
GO

/* =========================================================
   7. ChatSessions
   One conversation belongs to one user and usually one subject.
   ========================================================= */
IF OBJECT_ID(N'dbo.ChatSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatSessions
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        UserId      INT NULL,
        SubjectId   INT NULL,
        Title       NVARCHAR(255) NULL,
        CreatedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_ChatSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt   DATETIME2(0) NULL,

        CONSTRAINT FK_ChatSessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_ChatSessions_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id)
    );
END
GO

/* =========================================================
   8. ChatMessages
   Stores user questions and assistant answers.
   ========================================================= */
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ChatSessionId   INT NOT NULL,
        Role            NVARCHAR(30) NOT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        ModelName       NVARCHAR(100) NULL,
        PromptTokens    INT NULL,
        CompletionTokens INT NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_ChatMessages_ChatSessions FOREIGN KEY (ChatSessionId) REFERENCES dbo.ChatSessions(Id) ON DELETE CASCADE,
        CONSTRAINT CK_ChatMessages_Role CHECK (Role IN (N'User', N'Assistant', N'System'))
    );
END
GO

/* =========================================================
   9. Citations
   Links assistant answers to source chunks.
   ========================================================= */
IF OBJECT_ID(N'dbo.Citations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Citations
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ChatMessageId   INT NOT NULL,
        DocumentId      INT NOT NULL,
        ChunkId         INT NOT NULL,
        PageNumber      INT NULL,
        SlideNumber     INT NULL,
        SimilarityScore DECIMAL(9,6) NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Citations_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Citations_ChatMessages FOREIGN KEY (ChatMessageId) REFERENCES dbo.ChatMessages(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Citations_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.Documents(Id),
        CONSTRAINT FK_Citations_DocumentChunks FOREIGN KEY (ChunkId) REFERENCES dbo.DocumentChunks(Id)
    );
END
GO

/* =========================================================
   10. EvaluationQuestions
   Test set for 50 questions and ground-truth answers.
   ========================================================= */
IF OBJECT_ID(N'dbo.EvaluationQuestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvaluationQuestions
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        SubjectId       INT NOT NULL,
        Question        NVARCHAR(MAX) NOT NULL,
        GroundTruthAnswer NVARCHAR(MAX) NOT NULL,
        CreatedBy       INT NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_EvaluationQuestions_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_EvaluationQuestions_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(Id),
        CONSTRAINT FK_EvaluationQuestions_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id)
    );
END
GO

/* =========================================================
   11. RagasBenchmarkResults
   Stores RAGAS/benchmark results from experiments.
   ========================================================= */
IF OBJECT_ID(N'dbo.RagasBenchmarkResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RagasBenchmarkResults
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        EvaluationQuestionId INT NOT NULL,
        RunId               NVARCHAR(50) NOT NULL,
        EmbeddingModel      NVARCHAR(100) NOT NULL,
        LlmModel            NVARCHAR(100) NULL,
        VectorStore         NVARCHAR(50) NULL,
        ChunkingStrategy    NVARCHAR(100) NOT NULL,
        GeneratedAnswer     NVARCHAR(MAX) NULL,
        RetrievedContextsJson NVARCHAR(MAX) NULL,
        Faithfulness        DECIMAL(9,6) NULL,
        AnswerRelevancy     DECIMAL(9,6) NULL,
        ContextPrecision    DECIMAL(9,6) NULL,
        ContextRecall       DECIMAL(9,6) NULL,
        OverallScore        DECIMAL(9,6) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_RagasBenchmarkResults_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_RagasBenchmarkResults_EvaluationQuestions FOREIGN KEY (EvaluationQuestionId) REFERENCES dbo.EvaluationQuestions(Id) ON DELETE CASCADE
    );
END
GO

/* =========================================================
   Indexes
   ========================================================= */
CREATE INDEX IX_Subjects_SubjectName ON dbo.Subjects(SubjectName);
CREATE INDEX IX_Documents_SubjectId ON dbo.Documents(SubjectId);
CREATE INDEX IX_Documents_Status ON dbo.Documents(Status);
CREATE INDEX IX_DocumentChunks_DocumentId ON dbo.DocumentChunks(DocumentId);
CREATE INDEX IX_DocumentChunks_VectorId ON dbo.DocumentChunks(VectorId) WHERE VectorId IS NOT NULL;
CREATE INDEX IX_DocumentChunkEmbeddings_DocumentChunkId ON dbo.DocumentChunkEmbeddings(DocumentChunkId);
CREATE INDEX IX_DocumentChunkEmbeddings_VectorId ON dbo.DocumentChunkEmbeddings(VectorId) WHERE VectorId IS NOT NULL;
CREATE INDEX IX_DocumentConflictReviews_NewDocumentId ON dbo.DocumentConflictReviews(NewDocumentId);
CREATE INDEX IX_DocumentConflictReviews_SubjectId ON dbo.DocumentConflictReviews(SubjectId);
CREATE INDEX IX_DocumentConflictReviews_Status ON dbo.DocumentConflictReviews(Status);
CREATE INDEX IX_DocumentConflictCandidates_ReviewId ON dbo.DocumentConflictCandidates(ReviewId);
CREATE INDEX IX_DocumentConflictCandidates_CandidateDocumentId ON dbo.DocumentConflictCandidates(CandidateDocumentId);
CREATE INDEX IX_DocumentConflictFindings_CandidateId ON dbo.DocumentConflictFindings(CandidateId);
CREATE INDEX IX_DocumentConflictFindings_NewChunkId ON dbo.DocumentConflictFindings(NewChunkId);
CREATE INDEX IX_DocumentConflictFindings_ExistingChunkId ON dbo.DocumentConflictFindings(ExistingChunkId);
CREATE INDEX IX_ChatSessions_UserId ON dbo.ChatSessions(UserId);
CREATE INDEX IX_ChatSessions_SubjectId ON dbo.ChatSessions(SubjectId);
CREATE INDEX IX_ChatMessages_ChatSessionId ON dbo.ChatMessages(ChatSessionId);
CREATE INDEX IX_Citations_ChatMessageId ON dbo.Citations(ChatMessageId);
GO

/* =========================================================
   Seed data
   Demo login passwords are listed below after the user seed block.
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@chataiweb.local')
BEGIN
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES
    (N'Quản trị hệ thống', N'admin@chataiweb.local', N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViQWRtaW5fXw==$3G8Y7aSse368ibrsOFvz6jC1xHtRCogwHId2ACh66qg=', N'Admin'),
    (N'Giảng viên Demo', N'teacher@chataiweb.local', N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Sinh viên Demo', N'student@chataiweb.local', N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    -- Additional teachers (password: Teacher@123 — reuses the demo teacher hash)
    (N'Trần Văn An',     N'an.tran@chataiweb.local',     N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Nguyễn Thị Bình', N'binh.nguyen@chataiweb.local', N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Lê Văn Cường',    N'cuong.le@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    -- Additional students (password: Student@123 — reuses the demo student hash)
    (N'Phạm Thị Dung',   N'dung.pham@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Hoàng Văn Em',    N'em.hoang@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Vũ Thị Phương',   N'phuong.vu@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Đỗ Văn Giang',    N'giang.do@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Bùi Thị Hà',      N'ha.bui@chataiweb.local',      N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Ngô Văn Khoa',    N'khoa.ngo@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Lý Thị Lan',      N'lan.ly@chataiweb.local',      N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    -- More teachers (password: Teacher@123 — reuses the demo teacher hash)
    (N'Phan Thị Mai',    N'mai.phan@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Trịnh Văn Nam',   N'nam.trinh@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Đặng Thị Oanh',   N'oanh.dang@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Hồ Văn Phúc',     N'phuc.ho@chataiweb.local',     N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Võ Thị Quỳnh',    N'quynh.vo@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    (N'Lương Văn Sơn',   N'son.luong@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4=', N'Teacher'),
    -- More students (password: Student@123 — reuses the demo student hash)
    (N'Trương Thị Tâm',  N'tam.truong@chataiweb.local',  N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Đinh Văn Tuấn',   N'tuan.dinh@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Mai Thị Uyên',    N'uyen.mai@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Tô Văn Vũ',       N'vu.to@chataiweb.local',       N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Phùng Thị Xuân',  N'xuan.phung@chataiweb.local',  N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Cao Văn Đông',    N'dong.cao@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Hà Thị Anh',      N'anh.ha@chataiweb.local',      N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Dương Văn Bảo',   N'bao.duong@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Tạ Thị Châu',     N'chau.ta@chataiweb.local',     N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Lưu Văn Đạt',     N'dat.luu@chataiweb.local',     N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Tống Thị Hồng',   N'hong.tong@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Chu Văn Hùng',    N'hung.chu@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Nguyễn Văn Quân', N'quan.nguyen@chataiweb.local', N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Trần Thị Thảo',   N'thao.tran@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Lê Văn Tiến',     N'tien.le@chataiweb.local',     N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Phạm Thị Linh',   N'linh.pham@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Hoàng Văn Long',  N'long.hoang@chataiweb.local',  N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Vũ Thị Hương',    N'huong.vu@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Đỗ Văn Khánh',    N'khanh.do@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Bùi Thị Ngọc',    N'ngoc.bui@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Ngô Văn Phong',   N'phong.ngo@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Lý Thị Quyên',    N'quyen.ly@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Phan Văn Sang',   N'sang.phan@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Trịnh Thị Thu',   N'thu.trinh@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Đặng Văn Toàn',   N'toan.dang@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Hồ Thị Trang',    N'trang.ho@chataiweb.local',    N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Võ Văn Việt',     N'viet.vo@chataiweb.local',     N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Lương Thị Yến',   N'yen.luong@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Trương Văn Đức',  N'duc.truong@chataiweb.local',  N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Đinh Thị Hằng',   N'hang.dinh@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student'),
    (N'Mai Văn Khánh',   N'khanh.mai@chataiweb.local',   N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek=', N'Student');
END
GO

/* Demo login passwords:
   admin@chataiweb.local   / Admin@123
   teacher@chataiweb.local / Teacher@123
   student@chataiweb.local / Student@123
*/
UPDATE dbo.Users
SET FullName = N'Quản trị hệ thống',
    PasswordHash = N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViQWRtaW5fXw==$3G8Y7aSse368ibrsOFvz6jC1xHtRCogwHId2ACh66qg='
WHERE Email = N'admin@chataiweb.local';

UPDATE dbo.Users
SET FullName = N'Giảng viên Demo',
    PasswordHash = N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViVGVhY2hlcg==$MnyMqV23QsQhkLuV4ApM5LWVwasxCHniABxZxwb4uy4='
WHERE Email = N'teacher@chataiweb.local';

UPDATE dbo.Users
SET FullName = N'Sinh viên Demo',
    PasswordHash = N'PBKDF2-HMACSHA256$100000$Q2hhdEFJV2ViU3R1ZGVudA==$vZpTrUVuZTGibulis2S6aU9fpOMqt2N57ddjH2Gz+Ek='
WHERE Email = N'student@chataiweb.local';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectCode = N'CS101')
BEGIN
    INSERT INTO dbo.Subjects (SubjectCode, SubjectName, Description, CreatedBy)
    VALUES
    (N'CS101',   N'Nhập môn lập trình',          N'Tài liệu bài giảng nhập môn lập trình dùng để demo chatbot RAG.',          4),
    (N'DB201',   N'Cơ sở dữ liệu',               N'Tài liệu môn cơ sở dữ liệu, SQL, ERD và chuẩn hóa dữ liệu.',               5),
    (N'AI301',   N'Trí tuệ nhân tạo',            N'Tài liệu môn trí tuệ nhân tạo, machine learning và RAG.',                  6),
    (N'PRN222',  N'Lập trình ASP.NET Core MVC',  N'Tài liệu môn PRN222: ASP.NET Core MVC, EF Core, SignalR, kiến trúc 3-lớp.',14),
    (N'PRO192',  N'Lập trình hướng đối tượng',   N'Tài liệu môn lập trình hướng đối tượng với C#: lớp, kế thừa, đa hình.',    15),
    (N'CSI104',  N'Nhập môn ngành công nghệ thông tin', N'Tài liệu giới thiệu ngành CNTT, các nhánh chuyên môn và kỹ năng nền tảng.', 17),
    (N'SWE201',  N'Nhập môn kỹ thuật phần mềm',  N'Tài liệu môn kỹ thuật phần mềm: vòng đời SDLC, Agile, kiểm thử, quản lý cấu hình.', 18),
    (N'VNR202',  N'Lịch sử Đảng Cộng sản Việt Nam', N'Tài liệu môn Lịch sử Đảng: quá trình thành lập, các kỳ Đại hội và đường lối lãnh đạo cách mạng Việt Nam.', 2),
    (N'NWC203',  N'Mạng máy tính',                N'Tài liệu môn Mạng máy tính: mô hình OSI/TCP-IP, định tuyến, giao thức tầng ứng dụng và bảo mật mạng.', 6);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SubjectEnrollments)
BEGIN
    -- User Ids:
    --   Teachers (10): 2=Teacher Demo, 4=An, 5=Bình, 6=Cường, 14=Mai, 15=Nam, 16=Oanh, 17=Phúc, 18=Quỳnh, 19=Sơn
    --   Students (39): 3=Student Demo, 7=Dung, 8=Em, 9=Phương, 10=Giang, 11=Hà, 12=Khoa, 13=Lan,
    --                  20=Tâm, 21=Tuấn, 22=Uyên, 23=Vũ, 24=Xuân, 25=Đông, 26=Anh, 27=Bảo, 28=Châu, 29=Đạt,
    --                  30=Hồng, 31=Hùng, 32=Quân, 33=Thảo, 34=Tiến, 35=Linh, 36=Long, 37=Hương, 38=Khánh,
    --                  39=Ngọc, 40=Phong, 41=Quyên, 42=Sang, 43=Thu, 44=Toàn, 45=Trang, 46=Việt, 47=Yến,
    --                  48=Đức, 49=Hằng, 50=Khánh
    -- Subject Ids: 1=CS101, 2=DB201, 3=AI301, 4=PRN222, 5=PRO192, 6=CSI104, 7=SWE201, 8=VNR202, 9=NWC203
    INSERT INTO dbo.SubjectEnrollments (SubjectId, UserId, RoleInClass)
    VALUES
    -- CS101 — Nhập môn lập trình
    (1, 2, N'Teacher'), (1, 4, N'Teacher'), (1, 14, N'Teacher'),
    (1, 3, N'Student'), (1, 7, N'Student'), (1, 8, N'Student'), (1, 9, N'Student'),
    (1, 20, N'Student'), (1, 21, N'Student'), (1, 22, N'Student'), (1, 23, N'Student'),
    (1, 24, N'Student'), (1, 38, N'Student'),
    -- DB201 — Cơ sở dữ liệu
    (2, 2, N'Teacher'), (2, 5, N'Teacher'), (2, 15, N'Teacher'),
    (2, 3, N'Student'), (2, 10, N'Student'), (2, 11, N'Student'),
    (2, 25, N'Student'), (2, 26, N'Student'), (2, 27, N'Student'), (2, 28, N'Student'),
    (2, 39, N'Student'),
    -- AI301 — Trí tuệ nhân tạo
    (3, 2, N'Teacher'), (3, 6, N'Teacher'), (3, 16, N'Teacher'),
    (3, 3, N'Student'), (3, 7, N'Student'), (3, 12, N'Student'), (3, 13, N'Student'),
    (3, 29, N'Student'), (3, 30, N'Student'), (3, 31, N'Student'), (3, 32, N'Student'),
    (3, 40, N'Student'),
    -- PRN222 — ASP.NET Core MVC
    (4, 4, N'Teacher'), (4, 5, N'Teacher'), (4, 17, N'Teacher'),
    (4, 3, N'Student'), (4, 8, N'Student'), (4, 9, N'Student'), (4, 10, N'Student'), (4, 11, N'Student'),
    (4, 33, N'Student'), (4, 34, N'Student'), (4, 35, N'Student'), (4, 36, N'Student'),
    (4, 41, N'Student'),
    -- PRO192 — Lập trình hướng đối tượng
    (5, 4, N'Teacher'), (5, 14, N'Teacher'),
    (5, 3, N'Student'), (5, 7, N'Student'), (5, 9, N'Student'), (5, 12, N'Student'),
    (5, 20, N'Student'), (5, 25, N'Student'), (5, 33, N'Student'), (5, 42, N'Student'),
    (5, 43, N'Student'),
    -- CSI104 — Nhập môn ngành CNTT
    (6, 6, N'Teacher'), (6, 18, N'Teacher'),
    (6, 3, N'Student'), (6, 7, N'Student'), (6, 8, N'Student'), (6, 13, N'Student'),
    (6, 21, N'Student'), (6, 26, N'Student'), (6, 34, N'Student'), (6, 44, N'Student'),
    (6, 45, N'Student'),
    -- SWE201 — Nhập môn kỹ thuật phần mềm
    (7, 5, N'Teacher'), (7, 6, N'Teacher'), (7, 19, N'Teacher'),
    (7, 11, N'Student'), (7, 12, N'Student'), (7, 13, N'Student'),
    (7, 22, N'Student'), (7, 27, N'Student'), (7, 35, N'Student'), (7, 46, N'Student'),
    (7, 47, N'Student'), (7, 48, N'Student'),
    -- VNR202 — Lịch sử Đảng Cộng sản Việt Nam (môn đại cương — đông sinh viên)
    (8, 5, N'Teacher'), (8, 6, N'Teacher'), (8, 18, N'Teacher'), (8, 19, N'Teacher'),
    (8, 3, N'Student'), (8, 7, N'Student'), (8, 8, N'Student'), (8, 9, N'Student'),
    (8, 10, N'Student'), (8, 11, N'Student'), (8, 12, N'Student'), (8, 13, N'Student'),
    (8, 20, N'Student'), (8, 23, N'Student'), (8, 28, N'Student'), (8, 29, N'Student'),
    (8, 32, N'Student'), (8, 36, N'Student'), (8, 37, N'Student'), (8, 38, N'Student'),
    (8, 41, N'Student'), (8, 42, N'Student'), (8, 46, N'Student'), (8, 49, N'Student'),
    (8, 50, N'Student'),
    -- NWC203 — Mạng máy tính
    (9, 17, N'Teacher'), (9, 19, N'Teacher'),
    (9, 3, N'Student'), (9, 30, N'Student'), (9, 31, N'Student'), (9, 37, N'Student'),
    (9, 39, N'Student'), (9, 40, N'Student'), (9, 43, N'Student'), (9, 44, N'Student'),
    (9, 47, N'Student'), (9, 48, N'Student'), (9, 49, N'Student'), (9, 50, N'Student');
END
GO

/* =========================================================
   Seed: EvaluationQuestions
   Sample ground-truth Q&A used by the RAGAS benchmark page.
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EvaluationQuestions)
BEGIN
    INSERT INTO dbo.EvaluationQuestions (SubjectId, Question, GroundTruthAnswer, CreatedBy)
    VALUES
    -- CS101 — Nhập môn lập trình
    (1, N'Biến trong lập trình là gì?',
        N'Biến là vùng nhớ có tên dùng để lưu giá trị, có kiểu dữ liệu xác định và có thể thay đổi giá trị trong quá trình chạy chương trình.', 2),
    (1, N'Vòng lặp for và while khác nhau như thế nào?',
        N'Vòng lặp for thường dùng khi đã biết trước số lần lặp; vòng lặp while dùng khi điều kiện dừng phụ thuộc vào trạng thái chạy chương trình.', 2),
    (1, N'Hàm (function) trong lập trình dùng để làm gì?',
        N'Hàm là khối lệnh có tên, có thể nhận tham số và trả về kết quả, giúp tái sử dụng mã, tách logic và giảm trùng lặp.', 2),

    -- DB201 — Cơ sở dữ liệu
    (2, N'Khoá chính (primary key) là gì?',
        N'Khoá chính là một hoặc nhiều cột định danh duy nhất mỗi bản ghi trong bảng, không được trùng và không được NULL.', 2),
    (2, N'Chuẩn hoá 3NF nhằm mục đích gì?',
        N'Chuẩn 3NF loại bỏ các phụ thuộc bắc cầu giữa các thuộc tính không khoá vào khoá chính, giúp giảm dư thừa và tránh dị thường khi cập nhật.', 2),
    (2, N'Khác nhau giữa INNER JOIN và LEFT JOIN?',
        N'INNER JOIN chỉ trả về các bản ghi có khớp ở cả hai bảng; LEFT JOIN trả về toàn bộ bản ghi của bảng bên trái và NULL cho các cột bên phải khi không có khớp.', 2),

    -- AI301 — Trí tuệ nhân tạo
    (3, N'RAG (Retrieval-Augmented Generation) là gì?',
        N'RAG là kỹ thuật kết hợp truy xuất tài liệu liên quan với mô hình sinh ngôn ngữ, để mô hình trả lời dựa trên ngữ cảnh được truy xuất thay vì chỉ dựa vào tham số đã học.', 2),
    (3, N'Embedding vector trong NLP biểu diễn cái gì?',
        N'Embedding là vector số thực biểu diễn ngữ nghĩa của từ, câu hoặc tài liệu, sao cho các nội dung có ý nghĩa gần nhau sẽ có khoảng cách cosine nhỏ.', 2),
    (3, N'Cosine similarity được tính như thế nào?',
        N'Cosine similarity giữa hai vector A và B bằng tích vô hướng A·B chia cho tích độ dài |A|·|B|, kết quả nằm trong khoảng từ -1 đến 1.', 2),

    -- PRN222 — ASP.NET Core MVC
    (4, N'Vai trò của Controller trong mô hình MVC là gì?',
        N'Controller nhận yêu cầu HTTP, gọi service hoặc model để xử lý dữ liệu, sau đó chọn View hoặc kết quả trả về cho client; Controller nên giữ mỏng và không chứa nghiệp vụ.', 2),
    (4, N'Dependency Injection trong ASP.NET Core hoạt động như thế nào?',
        N'ASP.NET Core có sẵn container DI; các service được đăng ký qua AddScoped/AddSingleton/AddTransient trong Program.cs và được inject qua constructor của controller hoặc service.', 2),
    (4, N'Razor View và ViewModel khác nhau ở điểm nào?',
        N'Razor View là tệp .cshtml chịu trách nhiệm render HTML; ViewModel là lớp C# chỉ chứa dữ liệu và quy tắc validation cần thiết cho View, tách biệt khỏi entity nghiệp vụ.', 2),
    (4, N'SignalR dùng để làm gì trong ứng dụng web?',
        N'SignalR là thư viện cho phép server đẩy thông điệp thời gian thực tới client qua WebSocket (hoặc fallback), thường dùng cho chat, thông báo và cập nhật tiến trình.', 2),

    -- PRO192 — Lập trình hướng đối tượng
    (5, N'Bốn đặc tính cốt lõi của OOP là gì?',
        N'Bốn đặc tính của OOP gồm: đóng gói (encapsulation), kế thừa (inheritance), đa hình (polymorphism) và trừu tượng hoá (abstraction).', 4),
    (5, N'Interface và abstract class khác nhau ra sao trong C#?',
        N'Interface chỉ khai báo hành vi và một lớp có thể triển khai nhiều interface; abstract class có thể chứa cả phương thức trừu tượng lẫn cài đặt sẵn nhưng một lớp chỉ được kế thừa một abstract class.', 4),

    -- CSI104 — Nhập môn ngành CNTT
    (6, N'CPU và RAM khác nhau như thế nào?',
        N'CPU là bộ xử lý trung tâm, thực thi lệnh; RAM là bộ nhớ truy cập ngẫu nhiên, lưu tạm dữ liệu và lệnh đang chạy. Dữ liệu RAM mất khi tắt máy, CPU không lưu trữ dữ liệu lâu dài.', 6),
    (6, N'Phần mềm hệ thống khác phần mềm ứng dụng ra sao?',
        N'Phần mềm hệ thống (như hệ điều hành, driver) quản lý tài nguyên phần cứng và cung cấp nền tảng cho phần mềm khác; phần mềm ứng dụng phục vụ tác vụ cụ thể của người dùng cuối.', 6),

    -- SWE201 — Nhập môn kỹ thuật phần mềm
    (7, N'Vòng đời phát triển phần mềm (SDLC) gồm các giai đoạn nào?',
        N'SDLC thường gồm các giai đoạn: phân tích yêu cầu, thiết kế, lập trình, kiểm thử, triển khai và bảo trì.', 5),
    (7, N'Agile khác mô hình thác nước (Waterfall) như thế nào?',
        N'Waterfall thực hiện các giai đoạn tuần tự, hoàn tất giai đoạn trước rồi mới sang giai đoạn sau; Agile lặp theo các iteration ngắn, giao sản phẩm gia tăng và chấp nhận thay đổi yêu cầu liên tục.', 5),

    -- VNR202 — Lịch sử Đảng Cộng sản Việt Nam
    (8, N'Đảng Cộng sản Việt Nam được thành lập vào ngày tháng năm nào và ở đâu?',
        N'Đảng Cộng sản Việt Nam được thành lập ngày 3 tháng 2 năm 1930 tại Hương Cảng (Hồng Kông, Trung Quốc) thông qua Hội nghị hợp nhất ba tổ chức cộng sản, do Nguyễn Ái Quốc chủ trì.', 5),
    (8, N'Cương lĩnh chính trị đầu tiên của Đảng do ai soạn thảo và xác định những nội dung cốt lõi gì?',
        N'Cương lĩnh chính trị đầu tiên do Nguyễn Ái Quốc soạn thảo, xác định đường lối cách mạng Việt Nam là tiến hành cách mạng tư sản dân quyền và thổ địa cách mạng để đi tới xã hội cộng sản, với hai nhiệm vụ chiến lược là chống đế quốc và chống phong kiến, do giai cấp công nhân lãnh đạo thông qua Đảng.', 5),
    (8, N'Cách mạng Tháng Tám năm 1945 thành công có ý nghĩa lịch sử như thế nào?',
        N'Cách mạng Tháng Tám năm 1945 đập tan ách thống trị của thực dân Pháp và phát xít Nhật, lật đổ chế độ phong kiến, lập nên nước Việt Nam Dân chủ Cộng hoà ngày 2 tháng 9 năm 1945; mở ra kỷ nguyên độc lập dân tộc gắn liền với chủ nghĩa xã hội.', 5),
    (8, N'Đại hội đại biểu toàn quốc lần thứ VI của Đảng (năm 1986) có ý nghĩa gì?',
        N'Đại hội VI (tháng 12 năm 1986) khởi xướng đường lối Đổi mới toàn diện, chuyển từ nền kinh tế kế hoạch hoá tập trung bao cấp sang nền kinh tế hàng hoá nhiều thành phần vận hành theo cơ chế thị trường có sự quản lý của Nhà nước, theo định hướng xã hội chủ nghĩa.', 5),
    (8, N'Tư tưởng Hồ Chí Minh là gì và có vai trò như thế nào đối với cách mạng Việt Nam?',
        N'Tư tưởng Hồ Chí Minh là hệ thống quan điểm toàn diện và sâu sắc về những vấn đề cơ bản của cách mạng Việt Nam, kết quả của sự vận dụng và phát triển sáng tạo chủ nghĩa Mác – Lênin vào điều kiện cụ thể của Việt Nam, kế thừa tinh hoa văn hoá dân tộc và nhân loại; cùng với chủ nghĩa Mác – Lênin, đây là nền tảng tư tưởng và kim chỉ nam cho hành động của Đảng.', 5);
END
GO

PRINT N'ChatAIWebDb database script executed successfully.';
GO
