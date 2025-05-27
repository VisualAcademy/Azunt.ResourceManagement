------------------------------------------------------------------------------------------------------
--[!] 테이블(Table) 설계 - 리소스 관련 (최신)
-- 작성자: 박용준(redplus@live.com)
-- 타이틀: 닷넷코리아 - 리소스 관리  
-- 코멘트: 다중 애플리케이션 및 계층 리소스를 위한 확장 구조 반영
-- 작성일: 2021-03-19
-- 수정일: 2025-05-10 (DisplayOrder 컬럼 추가)
------------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Resources]
(
    [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,         -- 고유 식별자 (자동 증가)
    [Alias]          NVARCHAR(50) NULL,                              -- 리소스 이름(별칭): 예) Notice, Free, News 등
    [Route]          NVARCHAR(255) NULL,                             -- 라우팅 경로: 예) /Notice, /News 등
    [Title]          NVARCHAR(50) NULL,                              -- 리소스 제목: 예) 공지사항, 자유게시판 등
    [Description]    NVARCHAR(200) NULL,                             -- 리소스 설명(간단한 소개 또는 용도 설명)
    [SysopUserId]    NVARCHAR(50) NULL,                              -- 특정 리소스의 관리자(시삽) 사용자 ID
    [IsPublic]       BIT NULL DEFAULT ((1)),                         -- 공개 여부: 1(익명 접근 허용), 0(회원 전용)
    [GroupName]      NVARCHAR(50) NULL,                              -- 리소스 그룹 이름: 예) ReportWriter, SAT 등
    [GroupOrder]     INT NULL DEFAULT ((0)),                         -- 그룹 내 정렬 순서 (같은 GroupName 기준)
    [DisplayOrder]   INT NULL DEFAULT ((0)),                         -- 전체 또는 앱 내 표시 순서 (전역 정렬용)
    [MailEnable]     BIT NULL DEFAULT ((0)),                         -- 글 작성 시 메일 발송 여부
    [ShowList]       BIT NULL DEFAULT ((1)),                         -- 전체 리스트에서 노출 여부
    [MainShowList]   BIT DEFAULT(1),                                 -- 메인 페이지 요약 표시 여부
    [HeaderHtml]     NVARCHAR(MAX) NULL,                             -- 페이지 상단에 포함될 HTML 내용
    [FooterHtml]     NVARCHAR(MAX) NULL,                             -- 페이지 하단에 포함될 HTML 내용
    [AppName]        NVARCHAR(100) NULL DEFAULT('ReportWriter'),     -- 소속 애플리케이션 이름 (멀티앱 지원용)
    [Step]           INT NULL DEFAULT(0),                            -- 계층 구조 표현용 들여쓰기 수준
    [CreatedBy]      NVARCHAR(255) NULL,                             -- 생성자 사용자 ID 또는 이름
    [Created]        DATETIMEOFFSET NULL DEFAULT SYSDATETIMEOFFSET(),-- 생성 일시 (타임존 포함)
    [ModifiedBy]     NVARCHAR(255) NULL,                             -- 수정자 사용자 ID 또는 이름
    [Modified]       DATETIMEOFFSET NULL                             -- 마지막 수정 일시 (타임존 포함)
);
GO
