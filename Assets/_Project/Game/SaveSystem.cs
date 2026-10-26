using System;
using System.Globalization;
using System.IO;
using BitRunner.Core;
using UnityEngine;

namespace BitRunner.Game
{
    /// <summary>主菜单用来展示「继续游戏」的轻量摘要，不做完整反序列化校验。</summary>
    public struct SavePreview
    {
        public int Version;
        public int Week;
        public int Day;
        public string SavedAtUtc;
        public bool IsCompatible;
    }

    /// <summary>
    /// 存档的落盘层：JSON 编解码 + 文件读写。状态到数据的搬运在 Core 的
    /// <see cref="GameSave"/> 里，这里只负责「把 SaveData 变成磁盘上的字节」。
    ///
    /// 落盘策略：
    /// - 先写 <c>.tmp</c>，再把旧档备份成 <c>.backup.json</c>，最后替换主档；
    /// - 读档失败时自动回退到备份；
    /// - 存档只发生在基地（出征前自动存、回基地时存、离开基地时补存），
    ///   探索进行中的状态不落盘，所以读档永远回到基地。
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "bitsave.json";
        public const string BackupName = "bitsave.backup.json";

        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static string BackupPath
        {
            get { return Path.Combine(Application.persistentDataPath, BackupName); }
        }

        public static string LastError { get; private set; }

        public static DateTime LastSaveUtc { get; private set; }

        public static bool HasSave()
        {
            return File.Exists(SavePath) || File.Exists(BackupPath);
        }

        public static bool Save()
        {
            try
            {
                SaveData data = GameSession.ExportSave();
                data.Version = GameSave.CurrentVersion;
                data.SavedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                string json = JsonUtility.ToJson(data, true);
                if (string.IsNullOrEmpty(json))
                {
                    LastError = "序列化失败。";
                    return false;
                }

                string folder = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, json);

                if (File.Exists(SavePath))
                {
                    File.Copy(SavePath, BackupPath, true);
                    File.Delete(SavePath);
                }

                File.Move(temp, SavePath);

                LastSaveUtc = DateTime.UtcNow;
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                Debug.LogWarning("[BitRunner] 存档失败：" + exception);
                return false;
            }
        }

        public static bool TryLoad(out string error)
        {
            error = null;
            if (TryLoadFrom(SavePath, out error))
            {
                return true;
            }

            string primaryError = error;
            if (File.Exists(BackupPath) && TryLoadFrom(BackupPath, out error))
            {
                Debug.LogWarning("[BitRunner] 主存档不可用，已回退到备份：" + primaryError);
                return true;
            }

            error = primaryError;
            return false;
        }

        public static bool TryPeek(out SavePreview preview)
        {
            if (TryPeekFrom(SavePath, out preview))
            {
                return true;
            }

            return TryPeekFrom(BackupPath, out preview);
        }

        /// <summary>主菜单一行摘要；没有存档或无法解析时返回 null。</summary>
        public static string DescribeSave()
        {
            SavePreview preview;
            if (!TryPeek(out preview) || !preview.IsCompatible)
            {
                return null;
            }

            string text = "存档　第 " + preview.Week + " 周　第 " + preview.Day + "/7 天";

            DateTime savedAt;
            if (!string.IsNullOrEmpty(preview.SavedAtUtc)
                && DateTime.TryParse(
                    preview.SavedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out savedAt))
            {
                text += "　·　" + savedAt.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);
            }

            return text;
        }

        public static void DeleteSave()
        {
            TryDelete(SavePath);
            TryDelete(BackupPath);
            TryDelete(SavePath + ".tmp");
            LastError = null;
        }

        private static bool TryLoadFrom(string path, out string error)
        {
            error = null;
            if (!File.Exists(path))
            {
                error = "没有找到存档文件。";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                {
                    error = "存档文件是空的。";
                    return false;
                }

                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    error = "存档解析失败。";
                    return false;
                }

                if (!GameSession.ApplySaveData(data, out error))
                {
                    return false;
                }

                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static bool TryPeekFrom(string path, out SavePreview preview)
        {
            preview = default(SavePreview);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null)
                {
                    return false;
                }

                preview.Version = data.Version;
                preview.IsCompatible = data.Version == GameSave.CurrentVersion;
                preview.SavedAtUtc = data.SavedAtUtc;
                if (data.Campaign != null)
                {
                    preview.Week = data.Campaign.Week;
                    preview.Day = data.Campaign.Day;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[BitRunner] 删除存档失败：" + exception.Message);
            }
        }
    }
}
