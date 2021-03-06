DROP PROCEDURE [PopulateLargeList]
GO
DROP VIEW [LargeListWithCompute]
GO
DROP TABLE [LargeList]
GO
DROP VIEW [HeroCatalog]
GO
DROP TABLE [FullRoster]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [FullRoster](
	[_id] [int] IDENTITY(1,1) NOT NULL,
	[universe] [nvarchar](255) NOT NULL,
	[page_id] [int] NOT NULL,
	[name] [nvarchar](100) NOT NULL,
	[urlslug] [nvarchar](100) NOT NULL,
	[ID] [nvarchar](50) NULL,
	[ALIGN] [nvarchar](50) NULL,
	[EYE] [nvarchar](50) NULL,
	[HAIR] [nvarchar](50) NULL,
	[SEX] [nvarchar](50) NULL,
	[GSM] [nvarchar](50) NULL,
	[ALIVE] [nvarchar](50) NULL,
	[APPEARANCES] [int] NULL,
	[FIRST_APPEARANCE] [nvarchar](50) NULL,
	[Year] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/*
INSERT INTO dbo.FullRoster( universe, page_id, name, urlslug, ID, ALIGN, EYE, HAIR, SEX, GSM, ALIVE, APPEARANCES, FIRST_APPEARANCE, Year )
SELECT 'Marvel', page_id, name, urlslug, ID, ALIGN, EYE, HAIR, SEX, GSM, ALIVE, APPEARANCES, FIRST_APPEARANCE, Year FROM marvel.Roster UNION
SELECT 'DC', page_id, name, urlslug, ID, ALIGN, EYE, HAIR, SEX, GSM, ALIVE, APPEARANCES, FIRST_APPEARANCE, Year FROM dc.Roster
*/

CREATE VIEW [HeroCatalog] as 
	SELECT
		_id as Id, Universe, Name, appearances as Appeared, Year
	FROM
		dbo.FullRoster
	;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [LargeList](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[value] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [LargeListWithCompute] AS
	WITH 
		Base as (
			SELECT
				Id, Value, 
				CAST( CASE 
					WHEN Id <= 1 
						THEN (LEN(Value) * (Rand() + 1000) + Rand() + Rand() ) + 1 
						ELSE (LEN(Value) * (Rand(Id) * 1000) * Rand(Id-1) * Rand(Id+1)) + 1
				END as INT ) as Computed
			FROM
				dbo.LargeList
		)
	SELECT
		Base.Id, Base.Value,Base.Computed,
		HeroCatalog.Name,
		HeroCatalog.Universe,
		HeroCatalog.Appeared,
		HeroCatalog.Year
	FROM
		Base
	LEFT JOIN
		dbo.HeroCatalog
		ON HeroCatalog.Id = Base.Computed
;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [PopulateLargeList] AS 

DECLARE @iterations int = 1000000;
DECLARE @i int = 0;
DECLARE @value nvarchar(255);

WHILE( @i < @iterations ) BEGIN
  SET @value = 'Some Value ' + cast(@i AS nvarchar(25));

  INSERT INTO LargeList(value) VALUES (@value);

  SET @i = @i + 1;
END



SELECT
  *
FROM
  LargeList
;
GO
