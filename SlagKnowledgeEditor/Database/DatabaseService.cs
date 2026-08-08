using Microsoft.Data.Sqlite;
using SlagKnowledgeEditor.Models;
using System;
using System.IO;
using System.Windows;

namespace SlagKnowledgeEditor.Database
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService()
        {
            string databasePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "slag_knowledge.db");

            connectionString = $"Data Source={databasePath}";

            CreateDatabase();
        }


        private void CreateDatabase()
        {
            using SqliteConnection connection =
                new SqliteConnection(connectionString);

            connection.Open();

            // Создаём таблицу, если её ещё нет.
            string sql = @"
                CREATE TABLE IF NOT EXISTS DiagramRegions
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                    Al2O3 REAL NOT NULL,
                    Temperature INTEGER NOT NULL,

                    ImagePath TEXT NOT NULL,

                    TopLeftX REAL NOT NULL,
                    TopLeftY REAL NOT NULL,

                    TopRightX REAL NOT NULL,
                    TopRightY REAL NOT NULL,

                    BottomLeftX REAL NOT NULL,
                    BottomLeftY REAL NOT NULL,

                    BottomRightX REAL,
                    BottomRightY REAL,

                    FifthX REAL,
                    FifthY REAL,

                    UNIQUE(Al2O3, Temperature)
                );
            ";

            string calibrationSql = @"
    CREATE TABLE IF NOT EXISTS CompositionCalibrationPoints
    (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,

        Al2O3 REAL NOT NULL,
        Temperature INTEGER NOT NULL,

        X REAL NOT NULL,
        Y REAL NOT NULL,

        CaO REAL NOT NULL,
        MgO REAL NOT NULL,
        SiO2 REAL NOT NULL,

        UNIQUE(
            Al2O3,
            Temperature,
            X,
            Y
        )
    );
";

            using SqliteCommand calibrationCommand =
                new SqliteCommand(
                    calibrationSql,
                    connection);

            calibrationCommand.ExecuteNonQuery();

            using SqliteCommand command =
                new SqliteCommand(sql, connection);

            command.ExecuteNonQuery();


            // Если база была создана раньше,
            // добавляем новые столбцы в существующую таблицу.
            AddColumnIfNotExists(
                connection,
                "BottomRightX");

            AddColumnIfNotExists(
                connection,
                "BottomRightY");

            AddColumnIfNotExists(
                connection,
                "FifthX");

            AddColumnIfNotExists(
                connection,
                "FifthY");
        }


        private void AddColumnIfNotExists(
            SqliteConnection connection,
            string columnName)
        {
            string checkSql =
                "PRAGMA table_info(DiagramRegions);";

            using SqliteCommand checkCommand =
                new SqliteCommand(checkSql, connection);

            using SqliteDataReader reader =
                checkCommand.ExecuteReader();

            bool exists = false;

            while (reader.Read())
            {
                string name = reader.GetString(1);

                if (name == columnName)
                {
                    exists = true;
                    break;
                }
            }

            reader.Close();


            if (!exists)
            {
                string alterSql =
                    $"ALTER TABLE DiagramRegions " +
                    $"ADD COLUMN {columnName} REAL;";

                using SqliteCommand alterCommand =
                    new SqliteCommand(alterSql, connection);

                alterCommand.ExecuteNonQuery();
            }
        }


        // ============================================================
        // СОХРАНЕНИЕ ДИАГРАММЫ
        // ============================================================

        public void SaveDiagramRegion(DiagramRegion region)
        {
            using SqliteConnection connection =
                new SqliteConnection(connectionString);

            connection.Open();

            string sql = @"
                INSERT INTO DiagramRegions
                (
                    Al2O3,
                    Temperature,
                    ImagePath,

                    TopLeftX,
                    TopLeftY,

                    TopRightX,
                    TopRightY,

                    BottomLeftX,
                    BottomLeftY,

                    BottomRightX,
                    BottomRightY,

                    FifthX,
                    FifthY
                )
                VALUES
                (
                    @Al2O3,
                    @Temperature,
                    @ImagePath,

                    @TopLeftX,
                    @TopLeftY,

                    @TopRightX,
                    @TopRightY,

                    @BottomLeftX,
                    @BottomLeftY,

                    @BottomRightX,
                    @BottomRightY,

                    @FifthX,
                    @FifthY
                )

                ON CONFLICT(Al2O3, Temperature)
                DO UPDATE SET

                    ImagePath = excluded.ImagePath,

                    TopLeftX = excluded.TopLeftX,
                    TopLeftY = excluded.TopLeftY,

                    TopRightX = excluded.TopRightX,
                    TopRightY = excluded.TopRightY,

                    BottomLeftX = excluded.BottomLeftX,
                    BottomLeftY = excluded.BottomLeftY,

                    BottomRightX = excluded.BottomRightX,
                    BottomRightY = excluded.BottomRightY,

                    FifthX = excluded.FifthX,
                    FifthY = excluded.FifthY;
            ";

            using SqliteCommand command =
                new SqliteCommand(sql, connection);


            command.Parameters.AddWithValue(
                "@Al2O3",
                region.Al2O3);

            command.Parameters.AddWithValue(
                "@Temperature",
                region.Temperature);

            command.Parameters.AddWithValue(
                "@ImagePath",
                region.ImagePath);


            command.Parameters.AddWithValue(
                "@TopLeftX",
                region.TopLeftX);

            command.Parameters.AddWithValue(
                "@TopLeftY",
                region.TopLeftY);


            command.Parameters.AddWithValue(
                "@TopRightX",
                region.TopRightX);

            command.Parameters.AddWithValue(
                "@TopRightY",
                region.TopRightY);


            command.Parameters.AddWithValue(
                "@BottomLeftX",
                region.BottomLeftX);

            command.Parameters.AddWithValue(
                "@BottomLeftY",
                region.BottomLeftY);


            command.Parameters.AddWithValue(
                "@BottomRightX",
                region.BottomRightX.HasValue
                    ? region.BottomRightX.Value
                    : DBNull.Value);

            command.Parameters.AddWithValue(
                "@BottomRightY",
                region.BottomRightY.HasValue
                    ? region.BottomRightY.Value
                    : DBNull.Value);


            command.Parameters.AddWithValue(
                "@FifthX",
                region.FifthX.HasValue
                    ? region.FifthX.Value
                    : DBNull.Value);

            command.Parameters.AddWithValue(
                "@FifthY",
                region.FifthY.HasValue
                    ? region.FifthY.Value
                    : DBNull.Value);


            command.ExecuteNonQuery();
        }


        // ============================================================
        // ЗАГРУЗКА ДИАГРАММЫ
        // ============================================================

        public DiagramRegion? GetDiagramRegion(
            double al2o3,
            int temperature)
        {
            using SqliteConnection connection =
                new SqliteConnection(connectionString);

            connection.Open();

            string sql = @"
                SELECT
                    Id,
                    Al2O3,
                    Temperature,
                    ImagePath,

                    TopLeftX,
                    TopLeftY,

                    TopRightX,
                    TopRightY,

                    BottomLeftX,
                    BottomLeftY,

                    BottomRightX,
                    BottomRightY,

                    FifthX,
                    FifthY

                FROM DiagramRegions

                WHERE Al2O3 = @Al2O3
                  AND Temperature = @Temperature;
            ";

            using SqliteCommand command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@Al2O3",
                al2o3);

            command.Parameters.AddWithValue(
                "@Temperature",
                temperature);


            using SqliteDataReader reader =
                command.ExecuteReader();


            if (!reader.Read())
                return null;


            DiagramRegion region =
                new DiagramRegion
                {
                    Id = reader.GetInt32(0),

                    Al2O3 = reader.GetDouble(1),

                    Temperature = reader.GetInt32(2),

                    ImagePath = reader.GetString(3),

                    TopLeftX = reader.GetDouble(4),
                    TopLeftY = reader.GetDouble(5),

                    TopRightX = reader.GetDouble(6),
                    TopRightY = reader.GetDouble(7),

                    BottomLeftX = reader.GetDouble(8),
                    BottomLeftY = reader.GetDouble(9)
                };


            // 4-я точка
            if (!reader.IsDBNull(10))
            {
                region.BottomRightX =
                    reader.GetDouble(10);
            }

            if (!reader.IsDBNull(11))
            {
                region.BottomRightY =
                    reader.GetDouble(11);
            }


            // 5-я точка
            if (!reader.IsDBNull(12))
            {
                region.FifthX =
                    reader.GetDouble(12);
            }

            if (!reader.IsDBNull(13))
            {
                region.FifthY =
                    reader.GetDouble(13);
            }


            return region;
        }

        public void SaveCalibrationPoint(
    CompositionCalibrationPoint point)
        {
            using SqliteConnection connection =
                new SqliteConnection(connectionString);

            connection.Open();

            string sql = @"
        INSERT INTO CompositionCalibrationPoints
        (
            Al2O3,
            Temperature,
            X,
            Y,
            CaO,
            MgO,
            SiO2
        )
        VALUES
        (
            @Al2O3,
            @Temperature,
            @X,
            @Y,
            @CaO,
            @MgO,
            @SiO2
        )
        ON CONFLICT(
            Al2O3,
            Temperature,
            X,
            Y
        )
        DO UPDATE SET
            CaO = excluded.CaO,
            MgO = excluded.MgO,
            SiO2 = excluded.SiO2;
    ";

            using SqliteCommand command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@Al2O3",
                point.Al2O3);

            command.Parameters.AddWithValue(
                "@Temperature",
                point.Temperature);

            command.Parameters.AddWithValue(
                "@X",
                point.ImagePoint.X);

            command.Parameters.AddWithValue(
                "@Y",
                point.ImagePoint.Y);

            command.Parameters.AddWithValue(
                "@CaO",
                point.CaO);

            command.Parameters.AddWithValue(
                "@MgO",
                point.MgO);

            command.Parameters.AddWithValue(
                "@SiO2",
                point.SiO2);

            command.ExecuteNonQuery();
        }

        public List<CompositionCalibrationPoint>
    GetCalibrationPoints(
        double al2o3,
        int temperature)
        {
            List<CompositionCalibrationPoint> points = new();

            using SqliteConnection connection =
                new SqliteConnection(connectionString);

            connection.Open();

            string sql = @"
        SELECT
            Id,
            Al2O3,
            Temperature,
            X,
            Y,
            CaO,
            MgO,
            SiO2
        FROM CompositionCalibrationPoints
        WHERE Al2O3 = @Al2O3
          AND Temperature = @Temperature
        ORDER BY Id;
    ";

            using SqliteCommand command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@Al2O3",
                al2o3);

            command.Parameters.AddWithValue(
                "@Temperature",
                temperature);

            using SqliteDataReader reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                points.Add(
                    new CompositionCalibrationPoint
                    {
                        Id = reader.GetInt32(0),

                        Al2O3 = reader.GetDouble(1),

                        Temperature = reader.GetInt32(2),

                        ImagePoint = new Point(
                            reader.GetDouble(3),
                            reader.GetDouble(4)),

                        CaO = reader.GetDouble(5),

                        MgO = reader.GetDouble(6),

                        SiO2 = reader.GetDouble(7)
                    });
            }

            return points;
        }

        public void DeleteCalibrationPoints(
    double al2o3,
    int temperature)
        {
            using SqliteConnection connection =
                new SqliteConnection(connectionString);

            connection.Open();

            string sql = @"
        DELETE FROM CompositionCalibrationPoints
        WHERE Al2O3 = @Al2O3
          AND Temperature = @Temperature;
    ";

            using SqliteCommand command =
                new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@Al2O3",
                al2o3);

            command.Parameters.AddWithValue(
                "@Temperature",
                temperature);

            command.ExecuteNonQuery();
        }
    }
}