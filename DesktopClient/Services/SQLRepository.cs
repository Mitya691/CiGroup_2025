using DesktopClient.Model;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using DesktopClient.Helpers;

namespace DesktopClient.Services
{
    public class SQLRepository : ISQLRepository
    {
        private readonly string _cs;
        private readonly string _csElevatorDb;
        private readonly ILogger<SQLRepository> _logger;

        public SQLRepository(string connectionString, ILogger<SQLRepository> logger ,string csElevatorDb)
        {
            _cs = connectionString;
            _csElevatorDb = csElevatorDb;
            _logger = logger;
        }
        /// <summary>
        /// Собирает подготовленные данные для прокидывания на UI
        /// </summary>
        public async Task<List<Card>> GetLast30CardsAsync(CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            var sql = @"SELECT * FROM cards
                        ORDER BY EndTs DESC
                        LIMIT 30;";

            using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            using var r = await cmd.ExecuteReaderAsync(ct);

            var list = new List<Card>();
            while (await r.ReadAsync(ct))
            {
                list.Add(new Card
                {
                    Id = r.GetInt32("Id"),
                    StartTime = r.GetDateTime("StartTs"),
                    EndTime = r.GetDateTime("EndTs"),
                    Weight1 = r.IsDBNull("Weight1") ? (decimal?)null : r.GetDecimal("Weight1"),
                    Weight2 = r.IsDBNull("Weight2") ? (decimal?)null : r.GetDecimal("Weight2"),
                    TotalWeight = r.IsDBNull("TotalWeight") ? (decimal?)null : r.GetDecimal("TotalWeight"),
                    Direction = r.IsDBNull("Direction") ? null : r.GetString("Direction"),
                    SourceSilo = r.IsDBNull("SourceSilo") ? null : r.GetString("SourceSilo"),
                    TargetSilo = r.IsDBNull("TargetSilo") ? null : r.GetString("TargetSilo")
                });
            }
            return list;
        }

        /// <summary>
        /// Собирает только новые карточки после закрытия и добавляет их в список для прокидывания на UI
        /// </summary>
        /// <param name="lastEnd"></param>
        /// <param name="take"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Card>> GetCardsClosedAfterAsync(DateTime lastEnd, int take = 100, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"SELECT * FROM cards
                                 WHERE EndTs > @lastEnd
                                 ORDER BY EndTs ASC           
                                 LIMIT @take;";

            await using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@lastEnd", lastEnd);
            cmd.Parameters.AddWithValue("@take", take);

            await using var r = await cmd.ExecuteReaderAsync(ct);

            var list = new List<Card>();
            while (await r.ReadAsync(ct))
            {
                list.Add(new Card
                {
                    Id = r.GetInt32("Id"),
                    StartTime = r.GetDateTime("StartTs"),
                    EndTime = r.GetDateTime("EndTs"),
                    Weight1 = r.IsDBNull("Weight1") ? (decimal?)null : r.GetDecimal("Weight1"),
                    Weight2 = r.IsDBNull("Weight2") ? (decimal?)null : r.GetDecimal("Weight2"),
                    TotalWeight = r.IsDBNull("TotalWeight") ? (decimal?)null : r.GetDecimal("TotalWeight"),
                    Direction = r.IsDBNull("Direction") ? null : r.GetString("Direction"),
                    SourceSilo = r.IsDBNull("SourceSilo") ? null : r.GetString("SourceSilo"),
                    TargetSilo = r.IsDBNull("TargetSilo") ? null : r.GetString("TargetSilo")
                });
            }
            return list;
        }

        public async Task<List<Card>> GetCardsForInterval(DateTime? filterStart, DateTime? filterStop, CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            var sql = @"SELECT *
                        FROM cards
                        WHERE StartTs >= @filterStart AND EndTs <= @filterStop
                        ORDER BY EndTs DESC;";

            using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@filterStart", filterStart); 
            cmd.Parameters.AddWithValue("@filterStop", filterStop);
            using var r = await cmd.ExecuteReaderAsync(ct);

            var list = new List<Card>();
            while (await r.ReadAsync(ct))
            {
                list.Add(new Card
                {
                    Id = r.GetInt32("Id"),
                    StartTime = r.GetDateTime("StartTs"),
                    EndTime = r.GetDateTime("EndTs"),
                    Weight1 = r.IsDBNull("Weight1") ? (decimal?)null : r.GetDecimal("Weight1"),
                    Weight2 = r.IsDBNull("Weight2") ? (decimal?)null : r.GetDecimal("Weight2"),
                    TotalWeight = r.IsDBNull("TotalWeight") ? (decimal?)null : r.GetDecimal("TotalWeight"),
                    Direction = r.IsDBNull("Direction") ? null : r.GetString("Direction"),
                    SourceSilo = r.IsDBNull("SourceSilo") ? null : r.GetString("SourceSilo"),
                    TargetSilo = r.IsDBNull("TargetSilo") ? null : r.GetString("TargetSilo")
                });
            }
            return list;
        }

        /// <summary>
        /// Делает вставку подготовленных данных в чистую таблицу cards для удобной выборки
        /// </summary>
        /// <param name="lastEndInterval"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task InsertNewCard(DateTime lastEndInterval, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"INSERT INTO cards(TrendID, StartTs, EndTs, SourceSilo, Direction, TargetSilo, Weight1, Weight2)
                                    WITH c AS (
                                        SELECT TrendID, DateSet AS StartTs,
                                            LEAD(DateSet)  OVER (PARTITION BY TrendID ORDER BY DateSet) AS EndTs,
                                            TagValue, LEAD(TagValue) OVER (PARTITION BY TrendID ORDER BY DateSet) AS NextVal
                                        FROM int_archive
                                        WHERE DateSet >= @lastEndInterval - INTERVAL 1 DAY
                                    )

                                    SELECT c.TrendID, c.StartTs, c.EndTs,
                                        t.Name        AS SourceSilo,  
                                        t.Direction   AS Direction,   
                                        NULL          AS TargetSilo,   
                                        SUM(CASE WHEN d.TrendID = 1 THEN d.TagValue ELSE 0 END) AS Weight1,
                                        SUM(CASE WHEN d.TrendID = 2 THEN d.TagValue ELSE 0 END) AS Weight2  
                                    FROM c
                                    LEFT JOIN double_archive d
                                        ON d.DateSet >= c.StartTs
                                        AND d.DateSet <  c.EndTs
                                    LEFT JOIN trends t
                                        ON t.TagID = c.TrendID
                                        WHERE c.TagValue = 1 AND c.NextVal  = 0
                                              AND c.EndTs IS NOT NULL
                                              AND c.EndTs > @lastEndInterval
                                        GROUP BY c.TrendID, c.StartTs, c.EndTs, t.Name, t.Direction
                                        ORDER BY c.EndTs DESC
                                LIMIT 1;";

            await using var cmd = new MySqlCommand( sql, conn) { CommandTimeout = 5};
            cmd.Parameters.AddWithValue("@lastEndInterval", lastEndInterval);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>
        /// Перегрузка метода вставки карточки с учетом времени схода продукта
        /// </summary>
        /// <param name="lastEndInterval"></param>
        /// <param name="lagSec"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task InsertNewCard(DateTime lastEndInterval, int lagSec, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"INSERT INTO cards(TrendID, StartTs, EndTs, SourceSilo, Direction, TargetSilo, Weight1, Weight2)
                                    WITH c AS (
                                        SELECT TrendID, DateSet AS StartTs,
                                            LEAD(DateSet)  OVER (PARTITION BY TrendID ORDER BY DateSet) AS EndTs,
                                            TagValue, LEAD(TagValue) OVER (PARTITION BY TrendID ORDER BY DateSet) AS NextVal
                                        FROM int_archive
                                        WHERE DateSet >= @lastEndInterval - INTERVAL 1 DAY
                                    )
                                    SELECT c.TrendID, c.StartTs, c.EndTs,
                                        t.Name        AS SourceSilo,  
                                        t.Direction   AS Direction,   
                                        NULL          AS TargetSilo,   
                                        SUM(CASE WHEN d.TrendID = 1 THEN d.TagValue ELSE 0 END) AS Weight1,
                                        SUM(CASE WHEN d.TrendID = 2 THEN d.TagValue ELSE 0 END) AS Weight2  
                                    FROM c
                                    LEFT JOIN double_archive d
                                        ON d.DateSet >= c.StartTs + INTERVAL @lagSec SECOND
                                        AND d.DateSet <  c.EndTs + INTERVAL @lagSec SECOND
                                    LEFT JOIN trends t
                                        ON t.TagID = c.TrendID
                                        WHERE c.TagValue = 1 AND c.NextVal  = 0
                                              AND c.EndTs IS NOT NULL
                                              AND c.EndTs > @lastEndInterval
                                              AND c.EndTs + INTERVAL @lagSec SECOND <= (SELECT MAX(DateSet) FROM double_archive)
                                        GROUP BY c.TrendID, c.StartTs, c.EndTs, t.Name, t.Direction
                                        ORDER BY c.EndTs DESC
                                LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@lastEndInterval", lastEndInterval);
            cmd.Parameters.AddWithValue("@lagSec", lagSec);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task InsertNewCard(int lagSec, DateTime lastEndInterval, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"INSERT INTO cards
    (TrendID, StartTs, EndTs, SourceSilo, Direction, TargetSilo, Weight1, Weight2)
WITH c AS (
    SELECT
        TrendID,
        DateSet AS StartTs,
        LEAD(DateSet) OVER (PARTITION BY TrendID ORDER BY DateSet) AS EndTs,
        TagValue,
        LEAD(TagValue) OVER (PARTITION BY TrendID ORDER BY DateSet) AS NextVal
    FROM int_archive
    WHERE DateSet >= @lastEndInterval - INTERVAL 1 DAY
),
closed AS (
    SELECT *
    FROM c
    WHERE TagValue = 1
      AND NextVal  = 0
      AND EndTs IS NOT NULL
      AND EndTs > @lastEndInterval
    ORDER BY EndTs ASC
    LIMIT 1
)
SELECT
    cl.TrendID,
    cl.StartTs,
    cl.EndTs,
    t.Name      AS SourceSilo,
    t.Direction AS Direction,
    NULL        AS TargetSilo,

    -- Weight1 (TrendID = 1)
    GREATEST(
        -- endCounter: последнее значение до/на EndTs + lagSec
        IFNULL((
            SELECT d.TagValue
            FROM double_archive d
            WHERE d.TrendID = 1
              AND d.DateSet <= cl.EndTs + INTERVAL @lagSec SECOND
            ORDER BY d.DateSet DESC
            LIMIT 1
        ), 0)
        -
        -- startCounter:
        (
            CASE
                -- 1) если есть значения слева (<= StartTs) → берём последнее слева
                WHEN EXISTS (
                    SELECT 1
                    FROM double_archive d
                    WHERE d.TrendID = 1
                      AND d.DateSet <= cl.StartTs
                ) THEN (
                    SELECT d.TagValue
                    FROM double_archive d
                    WHERE d.TrendID = 1
                      AND d.DateSet <= cl.StartTs
                    ORDER BY d.DateSet DESC
                    LIMIT 1
                )

                -- 2) иначе берём самое близкое справа:
                --    первое значение > StartTs (можно ограничить до EndTs+lag, чтобы не улетать далеко)
                WHEN EXISTS (
                    SELECT 1
                    FROM double_archive d
                    WHERE d.TrendID = 1
                      AND d.DateSet > cl.StartTs
                      AND d.DateSet <= cl.EndTs + INTERVAL @lagSec SECOND
                ) THEN (
                    SELECT d.TagValue
                    FROM double_archive d
                    WHERE d.TrendID = 1
                      AND d.DateSet > cl.StartTs
                      AND d.DateSet <= cl.EndTs + INTERVAL @lagSec SECOND
                    ORDER BY d.DateSet ASC
                    LIMIT 1
                )

                -- 3) вообще нет измерений → 0
                ELSE 0
            END
        ),
        0
    ) AS Weight1,

    -- Weight2 (TrendID = 2) — аналогично
    GREATEST(
        IFNULL((
            SELECT d.TagValue
            FROM double_archive d
            WHERE d.TrendID = 2
              AND d.DateSet <= cl.EndTs + INTERVAL @lagSec SECOND
            ORDER BY d.DateSet DESC
            LIMIT 1
        ), 0)
        -
        (
            CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM double_archive d
                    WHERE d.TrendID = 2
                      AND d.DateSet <= cl.StartTs
                ) THEN (
                    SELECT d.TagValue
                    FROM double_archive d
                    WHERE d.TrendID = 2
                      AND d.DateSet <= cl.StartTs
                    ORDER BY d.DateSet DESC
                    LIMIT 1
                )
                WHEN EXISTS (
                    SELECT 1
                    FROM double_archive d
                    WHERE d.TrendID = 2
                      AND d.DateSet > cl.StartTs
                      AND d.DateSet <= cl.EndTs + INTERVAL @lagSec SECOND
                ) THEN (
                    SELECT d.TagValue
                    FROM double_archive d
                    WHERE d.TrendID = 2
                      AND d.DateSet > cl.StartTs
                      AND d.DateSet <= cl.EndTs + INTERVAL @lagSec SECOND
                    ORDER BY d.DateSet ASC
                    LIMIT 1
                )
                ELSE 0
            END
        ),
        0
    ) AS Weight2

FROM closed cl
LEFT JOIN trends t ON t.TagID = cl.TrendID;
";

            await using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@lastEndInterval", lastEndInterval);
            cmd.Parameters.AddWithValue("@lagSec", lagSec);
            try
            {
                var affected = await cmd.ExecuteNonQueryAsync(ct);
                _logger.LogInformation(
                    "InsertNewCard: inserted {Count} row(s) for lastEnd={LastEnd}, lagSec={LagSec}",
                    affected, lastEndInterval, lagSec);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex,
                    "InsertNewCard failed for lastEnd={LastEnd}, lagSec={LagSec}",
                    lastEndInterval, lagSec);
                throw;
            }
        }

        /// <summary>
        /// Метод обновляет целевой силос в карточке
        /// </summary>
        public async Task UpdateCardTargetSiloAsync(long id, string targetSilo, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            const string sql = @"UPDATE cards
                         SET TargetSilo=@target, UpdatedAt=NOW()
                         WHERE Id=@id;";

            await using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@target", targetSilo);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>
        /// Запрос проверяет наличие нового закрытого перегона, который еще не был обработан
        /// </summary>
        public async Task<bool> CheckCompletedIntervalAsync(DateTime lastEndInterval, CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            var sql = @" WITH e AS (
                            SELECT TrendID,
                            DateSet AS StartTs,
                            LEAD(DateSet)  OVER (PARTITION BY TrendID ORDER BY DateSet) AS EndTs,
                            TagValue,
                            LEAD(TagValue) OVER (PARTITION BY TrendID ORDER BY DateSet) AS NextVal
                            FROM int_archive)
                            SELECT EXISTS (
                            SELECT 1
                            FROM e
                            WHERE TagValue = 1    
                            AND NextVal  = 0       
                            AND EndTs IS NOT NULL    
                            AND EndTs > @lastEndInterval  
                            ) AS HasClosed;";   

            using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@lastEndInterval", lastEndInterval);

            using var r = await cmd.ExecuteReaderAsync(ct);

            await r.ReadAsync(ct);

            return r.GetBoolean("HasClosed");
        }

        /// <summary>
        /// Перегрузка метода проверки наличия нового закрытого перегона, с учетом временной задержки схода продукта
        /// </summary>
        /// <param name="lastEndInterval"></param>
        /// <param name="lagSec"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> CheckCompletedIntervalAsync(DateTime lastEndInterval, int lagSec, CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);

            var sql = @" WITH e AS (
                            SELECT TrendID,
                            DateSet AS StartTs,
                            LEAD(DateSet)  OVER (PARTITION BY TrendID ORDER BY DateSet) AS EndTs,
                            TagValue,
                            LEAD(TagValue) OVER (PARTITION BY TrendID ORDER BY DateSet) AS NextVal
                            FROM int_archive)
                            SELECT EXISTS (
                            SELECT 1
                            FROM e
                            WHERE TagValue = 1    
                            AND NextVal  = 0       
                            AND EndTs IS NOT NULL    
                            AND EndTs > @lastEndInterval  
                            AND EndTs + INTERVAL @lagSec SECOND <= (SELECT MAX(DateSet) FROM double_archive)
                            ) AS HasClosed;";

            using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 5 };
            cmd.Parameters.AddWithValue("@lastEndInterval", lastEndInterval);
            cmd.Parameters.AddWithValue("@lagSec", lagSec);

            using var r = await cmd.ExecuteReaderAsync(ct);

            await r.ReadAsync(ct);

            return r.GetBoolean("HasClosed");
        }

        public async Task<DateTime> GetMaxCardEndAsync(CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync(ct);
            const string sql = "SELECT COALESCE(MAX(EndTs), '1000-01-01') FROM cards;";
            await using var cmd = new MySqlCommand(sql, conn);
            var obj = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToDateTime(obj);
        }

        public async Task<List<string>> GetOperators(CancellationToken ct = default)
        {
            const string sql = @"
        SELECT Family, Name, Patronymic
        FROM users
        ORDER BY Family, Name, Patronymic;";

            var result = new List<string>();

            await using var conn = new MySqlConnection(_csElevatorDb);
            await conn.OpenAsync(ct);

            await using var cmd = new MySqlCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                // аккуратно с NULL'ами
                var family = reader.IsDBNull("Family") ? "" : reader.GetString("Family");
                var name = reader.IsDBNull("Name") ? "" : reader.GetString("Name");
                var patronymic = reader.IsDBNull("Patronymic") ? "" : reader.GetString("Patronymic");

                // склеиваем полное ФИО
                var fioFull = $"{family} {name} {patronymic}".Trim();

                if (string.IsNullOrWhiteSpace(fioFull))
                    continue;

                // твоя функция сокращения
                var fioShort = FioProcess.ToShortFio(fioFull); // или FioHelper.ToShortFio(fioFull);

                result.Add(fioShort);
            }

            return result;
        }
    }
}
