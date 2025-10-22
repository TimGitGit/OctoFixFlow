using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

namespace OctoFixFlow
{
    internal class DatabaseService
    {
        private readonly SQLiteConnection connection;
        private const string DatabaseFilePath = "DataService/octoFixFlow_data.db";
        public DatabaseService()
        {
            string directoryPath = Path.GetDirectoryName(DatabaseFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); // 创建缺失的目录
            }

            connection = new SQLiteConnection($"Data Source={DatabaseFilePath};");

            if (!File.Exists(DatabaseFilePath))
            {
                SQLiteConnection.CreateFile(DatabaseFilePath);
                InitializeDatabase();
            }
            try
            {
                connection.Open();
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"octoFixFlow_data.db open fail: {ex.Message}");
            }
        }
        private void InitializeDatabase()
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            string createTableQuery = @"  
                CREATE TABLE IF NOT EXISTS users (  
                    display_name VARCHAR(20) PRIMARY KEY,  
                    password_hash VARCHAR(20)  
                );";
            // 创建耗材设置表
            string createConsSettingsQuery = @"
                CREATE TABLE IF NOT EXISTS consumable_settings (
                    name TEXT PRIMARY KEY,            -- 耗材名称
                    id INTEGER,                       -- 序号
                    type INTEGER,                    -- 类型
                    description TEXT,                 -- 描述
                    NW INTEGER,                      -- 西北
                    SW INTEGER,                      -- 东北
                    NE INTEGER,                      -- 东南
                    SE INTEGER,                      -- 西南
                    numRows INTEGER,                 -- 行数
                    numColumns INTEGER,               -- 列数
                    labL REAL,                       -- 耗材长
                    labW REAL,                       -- 耗材宽
                    labH REAL,                       -- 耗材高
                    distanceRowY REAL,               -- A1孔中心距X的距离(mm)
                    distanceColumnX REAL,            -- A1孔中心距Y的距离(mm)
                    distanceRow REAL,                 -- 耗材孔间行距离(mm)
                    distanceColumn REAL,              -- 耗材孔间列距离(mm)
                    offsetX REAL,                    -- 耗材X的偏移量(mm)
                    offsetY REAL,                    -- 耗材Y的偏移量(mm)
                    RobotX REAL,                     -- 抓手x位置
                    RobotY REAL,                     -- 抓手y位置
                    RobotZ REAL,                     -- 抓手z位置
                    labVolume REAL,                  -- 孔容量(ul)
                    consMaxAvaiVol REAL,             -- 孔最大可用体积
                    consDep REAL,                    -- 孔深度
                    topShape INTEGER,                -- 顶部形状 0圆柱体 1立方体
                    topRadius REAL,                  -- 顶部半径(mm)
                    topUpperX REAL,                  -- 顶部长
                    topUpperY REAL,                  -- 顶部宽
                    TIPMAXCapacity REAL,             -- 最大容量
                    TIPMAXAvailable REAL,            -- 可用最大容量
                    TIPTotalLength REAL,             -- 枪头总长度
                    TIPHeadHeight REAL,              -- 枪头头部高度
                    TIPConeLength REAL,              -- 枪头圆锥长度
                    TIPMAXRadius REAL,               -- 最大半径
                    TIPMINRadius REAL,               -- 最小半径
                    TIPDepthOFComp REAL             -- 下压深度
                );";
            // 创建液体参数表
            string createLiquidSettingsQuery = @"
    CREATE TABLE IF NOT EXISTS liquid_settings (
        name TEXT PRIMARY KEY,            -- 液体名称（主键）
        description TEXT,                 -- 液体描述
        aisAirB REAL,                     -- 吸液前吸空气
        aisAirA REAL,                     -- 吸液后吸空气
        aisSpeed REAL,                    -- 吸液速度
        aisDelay REAL,                    -- 吸液延迟
        aisDistance REAL,                 -- 吸液距孔底距离
        disAirB REAL,                     -- 排液前吸空气
        disAirA REAL,                     -- 排液后吸空气
        disSpeed REAL,                    -- 排液速度
        disDelay REAL,                    -- 排液延迟
        disDistance REAL                  -- 排液距孔底距离
    );";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
                Console.WriteLine("Users table initialized.");
            }
            using (var command = new SQLiteCommand(createConsSettingsQuery, connection))
            {
                command.ExecuteNonQuery();
                Console.WriteLine("Consumable settings table initialized.");
            }
            using (var command = new SQLiteCommand(createLiquidSettingsQuery, connection))
            {
                command.ExecuteNonQuery();
                Console.WriteLine("Liquid settings table initialized.");
            }
            string checkUserQuery = "SELECT COUNT(*) FROM users";
            using (var command = new SQLiteCommand(checkUserQuery, connection))
            {
                var result = (long)command.ExecuteScalar();

                if (result == 0)
                {
                    string insertDefaultUser = @"INSERT INTO users (display_name, password_hash)   
                                                  VALUES (@displayName, @passwordHash)";
                    using (var insertCommand = new SQLiteCommand(insertDefaultUser, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@displayName", "Admin");
                        insertCommand.Parameters.AddWithValue("@passwordHash", "Admin");

                        insertCommand.ExecuteNonQuery();
                        Console.WriteLine("Default user inserted.");
                    }
                }
                else
                {
                    Console.WriteLine("Users table is not empty.");
                }
            }
        

        }
        public void Close()
        {
            connection.Close();
        }
        public async Task<bool> check_users_data(string displayName, string password)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Database connection must be open.");

                string query = "SELECT COUNT(*) FROM users WHERE display_name = @displayName AND password_hash = @password";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@displayName", displayName);
                    command.Parameters.AddWithValue("@password", password);  

                    var result = (long)command.ExecuteScalar();
                    return result > 0;
                }
            });
        }
        //修改密码
        public async Task<bool> update_users_password(string employeeName, string newPassword)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Database connection must be open.");

                // 使用参数化查询以防止 SQL 注入  
                string query = "UPDATE users SET password_hash = @newPassword WHERE display_name = @employeeName";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@employeeName", employeeName);
                    command.Parameters.AddWithValue("@newPassword", newPassword); // 注意：在实际应用中，密码应该经过哈希处理存储  

                    // 执行更新操作并返回受影响的行数  
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // 如果更新成功，返回 true  
                }
            });
        }

        //耗材类
        //新增耗材
        public async Task<bool> AddConsumableAsync(string name)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // 检查名称是否已存在
                string checkQuery = "SELECT COUNT(*) FROM consumable_settings WHERE name = @name";
                using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                        return false; // 名称已存在，新增失败
                }

                // 插入新记录（所有数值默认0）
                string insertQuery = @"
            INSERT INTO consumable_settings (
                name, id, type, description, NW, SW, NE, SE, numRows, numColumns,
                labL, labW, labH, distanceRowY, distanceColumnX, distanceRow, distanceColumn,
                offsetX, offsetY, RobotX, RobotY, RobotZ, labVolume, consMaxAvaiVol, consDep,
                topShape, topRadius, topUpperX, topUpperY, TIPMAXCapacity, TIPMAXAvailable,
                TIPTotalLength, TIPHeadHeight, TIPConeLength, TIPMAXRadius, TIPMINRadius, TIPDepthOFComp
            ) VALUES (
                @name, 0, 0, '', 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0
            )";

                using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@name", name);
                    int rowsAffected = insertCmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        //根据名称修改耗材信息
        public async Task<bool> UpdateConsumableAsync(ConsSettings settings)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string updateQuery = @"
            UPDATE consumable_settings SET
                id = @id,
                type = @type,
                description = @description,
                NW = @NW,
                SW = @SW,
                NE = @NE,
                SE = @SE,
                numRows = @numRows,
                numColumns = @numColumns,
                labL = @labL,
                labW = @labW,
                labH = @labH,
                distanceRowY = @distanceRowY,
                distanceColumnX = @distanceColumnX,
                distanceRow = @distanceRow,
                distanceColumn = @distanceColumn,
                offsetX = @offsetX,
                offsetY = @offsetY,
                RobotX = @RobotX,
                RobotY = @RobotY,
                RobotZ = @RobotZ,
                labVolume = @labVolume,
                consMaxAvaiVol = @consMaxAvaiVol,
                consDep = @consDep,
                topShape = @topShape,
                topRadius = @topRadius,
                topUpperX = @topUpperX,
                topUpperY = @topUpperY,
                TIPMAXCapacity = @TIPMAXCapacity,
                TIPMAXAvailable = @TIPMAXAvailable,
                TIPTotalLength = @TIPTotalLength,
                TIPHeadHeight = @TIPHeadHeight,
                TIPConeLength = @TIPConeLength,
                TIPMAXRadius = @TIPMAXRadius,
                TIPMINRadius = @TIPMINRadius,
                TIPDepthOFComp = @TIPDepthOFComp
            WHERE name = @name";

                using (var cmd = new SQLiteCommand(updateQuery, connection))
                {
                    // 绑定所有参数（与ConsSettings字段对应）
                    cmd.Parameters.AddWithValue("@name", settings.name);
                    cmd.Parameters.AddWithValue("@id", settings.id);
                    cmd.Parameters.AddWithValue("@type", settings.type);
                    cmd.Parameters.AddWithValue("@description", settings.description ?? ""); // 避免null
                    cmd.Parameters.AddWithValue("@NW", settings.NW);
                    cmd.Parameters.AddWithValue("@SW", settings.SW);
                    cmd.Parameters.AddWithValue("@NE", settings.NE);
                    cmd.Parameters.AddWithValue("@SE", settings.SE);
                    cmd.Parameters.AddWithValue("@numRows", settings.numRows);
                    cmd.Parameters.AddWithValue("@numColumns", settings.numColumns);
                    cmd.Parameters.AddWithValue("@labL", settings.labL);
                    cmd.Parameters.AddWithValue("@labW", settings.labW);
                    cmd.Parameters.AddWithValue("@labH", settings.labH);
                    cmd.Parameters.AddWithValue("@distanceRowY", settings.distanceRowY);
                    cmd.Parameters.AddWithValue("@distanceColumnX", settings.distanceColumnX);
                    cmd.Parameters.AddWithValue("@distanceRow", settings.distanceRow);
                    cmd.Parameters.AddWithValue("@distanceColumn", settings.distanceColumn);
                    cmd.Parameters.AddWithValue("@offsetX", settings.offsetX);
                    cmd.Parameters.AddWithValue("@offsetY", settings.offsetY);
                    cmd.Parameters.AddWithValue("@RobotX", settings.RobotX);
                    cmd.Parameters.AddWithValue("@RobotY", settings.RobotY);
                    cmd.Parameters.AddWithValue("@RobotZ", settings.RobotZ);
                    cmd.Parameters.AddWithValue("@labVolume", settings.labVolume);
                    cmd.Parameters.AddWithValue("@consMaxAvaiVol", settings.consMaxAvaiVol);
                    cmd.Parameters.AddWithValue("@consDep", settings.consDep);
                    cmd.Parameters.AddWithValue("@topShape", settings.topShape);
                    cmd.Parameters.AddWithValue("@topRadius", settings.topRadius);
                    cmd.Parameters.AddWithValue("@topUpperX", settings.topUpperX);
                    cmd.Parameters.AddWithValue("@topUpperY", settings.topUpperY);
                    cmd.Parameters.AddWithValue("@TIPMAXCapacity", settings.TIPMAXCapacity);
                    cmd.Parameters.AddWithValue("@TIPMAXAvailable", settings.TIPMAXAvailable);
                    cmd.Parameters.AddWithValue("@TIPTotalLength", settings.TIPTotalLength);
                    cmd.Parameters.AddWithValue("@TIPHeadHeight", settings.TIPHeadHeight);
                    cmd.Parameters.AddWithValue("@TIPConeLength", settings.TIPConeLength);
                    cmd.Parameters.AddWithValue("@TIPMAXRadius", settings.TIPMAXRadius);
                    cmd.Parameters.AddWithValue("@TIPMINRadius", settings.TIPMINRadius);
                    cmd.Parameters.AddWithValue("@TIPDepthOFComp", settings.TIPDepthOFComp);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        //修改名称
        public async Task<bool> UpdateConsumableNameAsync(string oldName, string newName)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // 1. 检查新名称是否已存在
                string checkNewNameQuery = "SELECT COUNT(*) FROM consumable_settings WHERE name = @newName";
                using (var checkCmd = new SQLiteCommand(checkNewNameQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@newName", newName);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                        return false; // 新名称已存在，更新失败
                }

                // 2. 检查旧名称是否存在
                string checkOldNameQuery = "SELECT COUNT(*) FROM consumable_settings WHERE name = @oldName";
                using (var checkOldCmd = new SQLiteCommand(checkOldNameQuery, connection))
                {
                    checkOldCmd.Parameters.AddWithValue("@oldName", oldName);
                    long oldCount = (long)checkOldCmd.ExecuteScalar();
                    if (oldCount == 0)
                        return false; // 旧名称不存在，无需更新
                }

                // 3. 执行名称更新（修改主键）
                string updateQuery = "UPDATE consumable_settings SET name = @newName WHERE name = @oldName";
                using (var cmd = new SQLiteCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@oldName", oldName);
                    cmd.Parameters.AddWithValue("@newName", newName);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        //获取所有的name
        public async Task<List<string>> GetAllConsumableNamesAsync()
        {
            return await Task.Run(() =>
            {
                var names = new List<string>();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = "SELECT name FROM consumable_settings ORDER BY name";
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0); // 读取第一列（name字段）
                        names.Add(name);
                    }
                }
                return names;
            });
        }
        //根据name获取耗材
        public async Task<ConsSettings> GetConsumableByNameAsync(string name)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = "SELECT * FROM consumable_settings WHERE name = @name";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null; // 未找到对应耗材

                        // 映射查询结果到ConsSettings对象
                        return new ConsSettings
                        {
                            name = reader.GetString(reader.GetOrdinal("name")),
                            id = reader.GetInt32(reader.GetOrdinal("id")),
                            type = reader.GetInt32(reader.GetOrdinal("type")),
                            description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("description")),
                            NW = reader.GetInt32(reader.GetOrdinal("NW")),
                            SW = reader.GetInt32(reader.GetOrdinal("SW")),
                            NE = reader.GetInt32(reader.GetOrdinal("NE")),
                            SE = reader.GetInt32(reader.GetOrdinal("SE")),
                            numRows = reader.GetInt32(reader.GetOrdinal("numRows")),
                            numColumns = reader.GetInt32(reader.GetOrdinal("numColumns")),
                            labL = reader.GetFloat(reader.GetOrdinal("labL")),
                            labW = reader.GetFloat(reader.GetOrdinal("labW")),
                            labH = reader.GetFloat(reader.GetOrdinal("labH")),
                            distanceRowY = reader.GetFloat(reader.GetOrdinal("distanceRowY")),
                            distanceColumnX = reader.GetFloat(reader.GetOrdinal("distanceColumnX")),
                            distanceRow = reader.GetFloat(reader.GetOrdinal("distanceRow")),
                            distanceColumn = reader.GetFloat(reader.GetOrdinal("distanceColumn")),
                            offsetX = reader.GetFloat(reader.GetOrdinal("offsetX")),
                            offsetY = reader.GetFloat(reader.GetOrdinal("offsetY")),
                            RobotX = reader.GetFloat(reader.GetOrdinal("RobotX")),
                            RobotY = reader.GetFloat(reader.GetOrdinal("RobotY")),
                            RobotZ = reader.GetFloat(reader.GetOrdinal("RobotZ")),
                            labVolume = reader.GetFloat(reader.GetOrdinal("labVolume")),
                            consMaxAvaiVol = reader.GetFloat(reader.GetOrdinal("consMaxAvaiVol")),
                            consDep = reader.GetFloat(reader.GetOrdinal("consDep")),
                            topShape = reader.GetInt32(reader.GetOrdinal("topShape")),
                            topRadius = reader.GetFloat(reader.GetOrdinal("topRadius")),
                            topUpperX = reader.GetFloat(reader.GetOrdinal("topUpperX")),
                            topUpperY = reader.GetFloat(reader.GetOrdinal("topUpperY")),
                            TIPMAXCapacity = reader.GetFloat(reader.GetOrdinal("TIPMAXCapacity")),
                            TIPMAXAvailable = reader.GetFloat(reader.GetOrdinal("TIPMAXAvailable")),
                            TIPTotalLength = reader.GetFloat(reader.GetOrdinal("TIPTotalLength")),
                            TIPHeadHeight = reader.GetFloat(reader.GetOrdinal("TIPHeadHeight")),
                            TIPConeLength = reader.GetFloat(reader.GetOrdinal("TIPConeLength")),
                            TIPMAXRadius = reader.GetFloat(reader.GetOrdinal("TIPMAXRadius")),
                            TIPMINRadius = reader.GetFloat(reader.GetOrdinal("TIPMINRadius")),
                            TIPDepthOFComp = reader.GetFloat(reader.GetOrdinal("TIPDepthOFComp"))
                        };
                    }
                }
            });
        }
        //根据name删除耗材
        public async Task<bool> DeleteConsumableAsync(string name)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // 检查耗材是否存在
                string checkQuery = "SELECT COUNT(*) FROM consumable_settings WHERE name = @name";
                using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count == 0)
                        return false; // 耗材不存在，删除失败
                }

                // 执行删除
                string deleteQuery = "DELETE FROM consumable_settings WHERE name = @name";
                using (var cmd = new SQLiteCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0; // 成功删除返回true
                }
            });
        }
        /// <summary>
        /// 获取所有耗材的完整信息（包含所有参数）
        /// </summary>
        /// <returns>所有耗材的ConsSettings对象集合</returns>
        public async Task<List<ConsSettings>> GetAllConsumablesAsync()
        {
            return await Task.Run(() =>
            {
                var allConsumables = new List<ConsSettings>(); // 存储所有耗材的集合

                if (connection.State != ConnectionState.Open)
                    connection.Open(); // 确保连接已打开

                // 查询所有耗材记录（按名称排序，方便前端展示）
                string query = "SELECT * FROM consumable_settings ORDER BY name";

                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) // 遍历所有记录
                    {
                        // 将每条记录映射为ConsSettings对象
                        var consumable = new ConsSettings
                        {
                            name = reader.GetString(reader.GetOrdinal("name")),
                            id = reader.GetInt32(reader.GetOrdinal("id")),
                            type = reader.GetInt32(reader.GetOrdinal("type")),
                            description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? "" // 处理NULL值，默认为空字符串
                                : reader.GetString(reader.GetOrdinal("description")),
                            NW = reader.GetInt32(reader.GetOrdinal("NW")),
                            SW = reader.GetInt32(reader.GetOrdinal("SW")),
                            NE = reader.GetInt32(reader.GetOrdinal("NE")),
                            SE = reader.GetInt32(reader.GetOrdinal("SE")),
                            numRows = reader.GetInt32(reader.GetOrdinal("numRows")),
                            numColumns = reader.GetInt32(reader.GetOrdinal("numColumns")),
                            labL = reader.GetFloat(reader.GetOrdinal("labL")),
                            labW = reader.GetFloat(reader.GetOrdinal("labW")),
                            labH = reader.GetFloat(reader.GetOrdinal("labH")),
                            distanceRowY = reader.GetFloat(reader.GetOrdinal("distanceRowY")),
                            distanceColumnX = reader.GetFloat(reader.GetOrdinal("distanceColumnX")),
                            distanceRow = reader.GetFloat(reader.GetOrdinal("distanceRow")),
                            distanceColumn = reader.GetFloat(reader.GetOrdinal("distanceColumn")),
                            offsetX = reader.GetFloat(reader.GetOrdinal("offsetX")),
                            offsetY = reader.GetFloat(reader.GetOrdinal("offsetY")),
                            RobotX = reader.GetFloat(reader.GetOrdinal("RobotX")),
                            RobotY = reader.GetFloat(reader.GetOrdinal("RobotY")),
                            RobotZ = reader.GetFloat(reader.GetOrdinal("RobotZ")),
                            labVolume = reader.GetFloat(reader.GetOrdinal("labVolume")),
                            consMaxAvaiVol = reader.GetFloat(reader.GetOrdinal("consMaxAvaiVol")),
                            consDep = reader.GetFloat(reader.GetOrdinal("consDep")),
                            topShape = reader.GetInt32(reader.GetOrdinal("topShape")),
                            topRadius = reader.GetFloat(reader.GetOrdinal("topRadius")),
                            topUpperX = reader.GetFloat(reader.GetOrdinal("topUpperX")),
                            topUpperY = reader.GetFloat(reader.GetOrdinal("topUpperY")),
                            TIPMAXCapacity = reader.GetFloat(reader.GetOrdinal("TIPMAXCapacity")),
                            TIPMAXAvailable = reader.GetFloat(reader.GetOrdinal("TIPMAXAvailable")),
                            TIPTotalLength = reader.GetFloat(reader.GetOrdinal("TIPTotalLength")),
                            TIPHeadHeight = reader.GetFloat(reader.GetOrdinal("TIPHeadHeight")),
                            TIPConeLength = reader.GetFloat(reader.GetOrdinal("TIPConeLength")),
                            TIPMAXRadius = reader.GetFloat(reader.GetOrdinal("TIPMAXRadius")),
                            TIPMINRadius = reader.GetFloat(reader.GetOrdinal("TIPMINRadius")),
                            TIPDepthOFComp = reader.GetFloat(reader.GetOrdinal("TIPDepthOFComp"))
                        };

                        allConsumables.Add(consumable); // 添加到集合
                    }
                }

                return allConsumables; // 返回所有耗材
            });
        }
        /// <summary>
        /// 新增液体参数（名称不存在时创建）
        /// </summary>
        public async Task<bool> AddLiquidAsync(string name)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // 检查名称是否已存在
                string checkQuery = "SELECT COUNT(*) FROM liquid_settings WHERE name = @name";
                using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                        return false; // 名称已存在，新增失败
                }

                // 插入新记录（所有数值默认0）
                string insertQuery = @"
            INSERT INTO liquid_settings (
                name, description, aisAirB, aisAirA, aisSpeed, aisDelay, aisDistance,
                disAirB, disAirA, disSpeed, disDelay, disDistance
            ) VALUES (
                @name, '', 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0
            )";

                using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@name", name);
                    int rowsAffected = insertCmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        /// <summary>
        /// 根据名称更新液体参数信息
        /// </summary>
        public async Task<bool> UpdateLiquidAsync(LiquidSettings settings)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string updateQuery = @"
            UPDATE liquid_settings SET
                description = @description,
                aisAirB = @aisAirB,
                aisAirA = @aisAirA,
                aisSpeed = @aisSpeed,
                aisDelay = @aisDelay,
                aisDistance = @aisDistance,
                disAirB = @disAirB,
                disAirA = @disAirA,
                disSpeed = @disSpeed,
                disDelay = @disDelay,
                disDistance = @disDistance
            WHERE name = @name";

                using (var cmd = new SQLiteCommand(updateQuery, connection))
                {
                    // 绑定所有参数（与LiquidSettings字段对应）
                    cmd.Parameters.AddWithValue("@name", settings.name);
                    cmd.Parameters.AddWithValue("@description", settings.description ?? ""); // 避免null
                    cmd.Parameters.AddWithValue("@aisAirB", settings.aisAirB);
                    cmd.Parameters.AddWithValue("@aisAirA", settings.aisAirA);
                    cmd.Parameters.AddWithValue("@aisSpeed", settings.aisSpeed);
                    cmd.Parameters.AddWithValue("@aisDelay", settings.aisDelay);
                    cmd.Parameters.AddWithValue("@aisDistance", settings.aisDistance);
                    cmd.Parameters.AddWithValue("@disAirB", settings.disAirB);
                    cmd.Parameters.AddWithValue("@disAirA", settings.disAirA);
                    cmd.Parameters.AddWithValue("@disSpeed", settings.disSpeed);
                    cmd.Parameters.AddWithValue("@disDelay", settings.disDelay);
                    cmd.Parameters.AddWithValue("@disDistance", settings.disDistance);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        /// <summary>
        /// 修改液体名称（需确保新名称不存在）
        /// </summary>
        public async Task<bool> UpdateLiquidNameAsync(string oldName, string newName)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // 检查新名称是否已存在
                string checkNewNameQuery = "SELECT COUNT(*) FROM liquid_settings WHERE name = @newName";
                using (var checkCmd = new SQLiteCommand(checkNewNameQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@newName", newName);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                        return false; // 新名称已存在，更新失败
                }

                // 检查旧名称是否存在
                string checkOldNameQuery = "SELECT COUNT(*) FROM liquid_settings WHERE name = @oldName";
                using (var checkOldCmd = new SQLiteCommand(checkOldNameQuery, connection))
                {
                    checkOldCmd.Parameters.AddWithValue("@oldName", oldName);
                    long oldCount = (long)checkOldCmd.ExecuteScalar();
                    if (oldCount == 0)
                        return false; // 旧名称不存在，无需更新
                }

                // 执行名称更新
                string updateQuery = "UPDATE liquid_settings SET name = @newName WHERE name = @oldName";
                using (var cmd = new SQLiteCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@oldName", oldName);
                    cmd.Parameters.AddWithValue("@newName", newName);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        /// <summary>
        /// 获取所有液体名称列表
        /// </summary>
        public async Task<List<string>> GetAllLiquidNamesAsync()
        {
            return await Task.Run(() =>
            {
                var names = new List<string>();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = "SELECT name FROM liquid_settings ORDER BY name";
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader.GetString(0); // 读取name字段
                        names.Add(name);
                    }
                }
                return names;
            });
        }
        /// <summary>
        /// 根据名称获取液体参数详情
        /// </summary>
        public async Task<LiquidSettings> GetLiquidByNameAsync(string name)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = "SELECT * FROM liquid_settings WHERE name = @name";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null; // 未找到对应液体

                        // 映射查询结果到LiquidSettings对象
                        return new LiquidSettings
                        {
                            name = reader.GetString(reader.GetOrdinal("name")),
                            description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("description")),
                            aisAirB = reader.GetFloat(reader.GetOrdinal("aisAirB")),
                            aisAirA = reader.GetFloat(reader.GetOrdinal("aisAirA")),
                            aisSpeed = reader.GetFloat(reader.GetOrdinal("aisSpeed")),
                            aisDelay = reader.GetFloat(reader.GetOrdinal("aisDelay")),
                            aisDistance = reader.GetFloat(reader.GetOrdinal("aisDistance")),
                            disAirB = reader.GetFloat(reader.GetOrdinal("disAirB")),
                            disAirA = reader.GetFloat(reader.GetOrdinal("disAirA")),
                            disSpeed = reader.GetFloat(reader.GetOrdinal("disSpeed")),
                            disDelay = reader.GetFloat(reader.GetOrdinal("disDelay")),
                            disDistance = reader.GetFloat(reader.GetOrdinal("disDistance"))
                        };
                    }
                }
            });
        }
        /// <summary>
        /// 根据名称删除液体参数
        /// </summary>
        public async Task<bool> DeleteLiquidAsync(string name)
        {
            return await Task.Run(() =>
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // 检查液体是否存在
                string checkQuery = "SELECT COUNT(*) FROM liquid_settings WHERE name = @name";
                using (var checkCmd = new SQLiteCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count == 0)
                        return false; // 液体不存在，删除失败
                }

                // 执行删除
                string deleteQuery = "DELETE FROM liquid_settings WHERE name = @name";
                using (var cmd = new SQLiteCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
        /// <summary>
        /// 获取所有液体参数的完整信息
        /// </summary>
        public async Task<List<LiquidSettings>> GetAllLiquidsAsync()
        {
            return await Task.Run(() =>
            {
                var allLiquids = new List<LiquidSettings>();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = "SELECT * FROM liquid_settings ORDER BY name";
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var liquid = new LiquidSettings
                        {
                            name = reader.GetString(reader.GetOrdinal("name")),
                            description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("description")),
                            aisAirB = reader.GetFloat(reader.GetOrdinal("aisAirB")),
                            aisAirA = reader.GetFloat(reader.GetOrdinal("aisAirA")),
                            aisSpeed = reader.GetFloat(reader.GetOrdinal("aisSpeed")),
                            aisDelay = reader.GetFloat(reader.GetOrdinal("aisDelay")),
                            aisDistance = reader.GetFloat(reader.GetOrdinal("aisDistance")),
                            disAirB = reader.GetFloat(reader.GetOrdinal("disAirB")),
                            disAirA = reader.GetFloat(reader.GetOrdinal("disAirA")),
                            disSpeed = reader.GetFloat(reader.GetOrdinal("disSpeed")),
                            disDelay = reader.GetFloat(reader.GetOrdinal("disDelay")),
                            disDistance = reader.GetFloat(reader.GetOrdinal("disDistance"))
                        };
                        allLiquids.Add(liquid);
                    }
                }
                return allLiquids;
            });
        }
    }
}
