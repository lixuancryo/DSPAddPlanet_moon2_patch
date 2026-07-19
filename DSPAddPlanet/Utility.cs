using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPAddPlanet
{
    static class Utility
    {
        static public Dictionary<string, string> ParseQueryString (string queryString)
        {
            Dictionary<string, string> parameterMap = new Dictionary<string, string>();
            string[] splittedQueryString = queryString.Split('&');
            foreach (string s in splittedQueryString)
            {
                string[] pair = s.Split('=');
                parameterMap[pair[0].Trim()] = pair[1].Trim();
            }
            return parameterMap;
        }

        static public string UniqueStarIdWithGameName (string gameName, string clusterString, string starName)
        {
            return gameName + '.' + clusterString + '.' + starName;
        }

        static public string UniqueStarIdWithGameName (string gameName, string clusterString, int starId)
        {
            return gameName + '.' + clusterString + ".StarId#" + starId;
        }

        static public string UniqueStarIdWithoutGameName (string clusterString, string starName)
        {
            return clusterString + '.' + starName;
        }

        static public string UniqueStarIdWithoutGameName (string clusterString, int starId)
        {
            return clusterString + ".StarId#" + starId;
        }

        static public void PrintThemeTable ()
        {
            //string title = "| ID | name | planet type | temperature | gas items | gas speeds | wind | ion height | water height | water item | culling radius | ice flag |\r\n" +
            //              "| --- | ---- | ----------- | ----------- | --------- | ---------- | ---- | ---------- | ------------ | ---------- | -------------- | -------- |\r\n";
            string title = "| ID | 名称 | 行星类型 | 温度 | 气体种类 | 产气速度 | 风 | ion height | 海面高度 | 海洋类型 | culling radius | ice flag |\r\n" +
                          "| --- | ---- | ----------- | ----------- | --------- | ---------- | ---- | ---------- | ------------ | ---------- | -------------- | -------- |\r\n";

            StringBuilder table = new StringBuilder(title);

            List<ThemeProto> themeProtos = LDB.themes.dataArray.ToList();
            themeProtos.Sort((a, b) => a.ID - b.ID);

            foreach (ThemeProto theme in themeProtos)
            {
                StringBuilder gasItems = new StringBuilder();
                if (theme.GasItems.Length > 0)
                {
                    for (int i = 0; i < theme.GasItems.Length; ++i)
                    {
                        ItemProto itemProto = LDB.items.Select(theme.GasItems[i]);

                        gasItems.Append(itemProto.Name.Translate());
                        //gasItems.Append(itemProto.Name);

                        if (i < theme.GasItems.Length - 1)
                        {
                            gasItems.Append(", ");
                        }
                    }
                }

                StringBuilder gasSpeeds = new StringBuilder();
                if (theme.GasSpeeds.Length > 0)
                {
                    for (int i = 0; i < theme.GasSpeeds.Length; ++i)
                    {
                        gasSpeeds.Append(theme.GasSpeeds[i]);
                        if (i < theme.GasSpeeds.Length - 1)
                        {
                            gasSpeeds.Append(", ");
                        }
                    }
                }

                ItemProto waterItemProto = LDB.items.Select(theme.WaterItemId);

                string waterItem = waterItemProto == null ? "" : waterItemProto.Name.Translate();
                //string waterItem = waterItemProto == null ? "" : waterItemProto.Name;

                string name = theme.DisplayName.Translate();
                //string name = theme.DisplayName;

                table.Append($"| {theme.ID} | {name} | {theme.PlanetType} | {theme.Temperature} | {gasItems} | {gasSpeeds} | {theme.Wind} | {theme.IonHeight} | {theme.WaterHeight} | {waterItem} | {theme.CullingRadius} | {theme.IceFlag} |\r\n");
            }

            Plugin.Instance.Logger.LogInfo("\r\n" + table);
        }

        static public string EnumValuesJoin<T> () where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            List<string> strings = new List<string>();
            foreach (object v in values)
            {
                strings.Add(v.ToString());
            }
            return strings.Join();
        }

        /// <summary>
        /// 根据当前的游戏名称、clusterString、恒星 ID 和旧版恒星名称获取行星配置信息，如果未找到则返回 null
        /// </summary>
        /// <param name="gameName">当前的游戏名称</param>
        /// <param name="clusterString"></param>
        /// <param name="starId">当前恒星的一基 ID</param>
        /// <param name="starName"></param>
        /// <param name="globalConfig"></param>
        /// <param name="gameNameSpecificConfig"></param>
        /// <param name="uniqueStarId"></param>
        /// <returns></returns>
        static public List<AdditionalPlanetConfig> GetPlanetConfigList (
            string gameName,
            string clusterString,
            int starId,
            string starName,
            Dictionary<string, List<AdditionalPlanetConfig>> globalConfig,
            Dictionary<string, List<AdditionalPlanetConfig>> gameNameSpecificConfig,
            out string uniqueStarId
        )
        {
            if (globalConfig.Count == 0 && gameNameSpecificConfig.Count == 0)
            {
                uniqueStarId = null;
                return null;
            }

            if (string.IsNullOrWhiteSpace(gameName))
            {
                uniqueStarId = UniqueStarIdWithoutGameName(clusterString, starId);
                if (globalConfig.ContainsKey(uniqueStarId))
                {
                    return globalConfig[uniqueStarId];
                }

                if (!string.IsNullOrWhiteSpace(starName))
                {
                    uniqueStarId = UniqueStarIdWithoutGameName(clusterString, starName);
                    if (globalConfig.ContainsKey(uniqueStarId))
                    {
                        return globalConfig[uniqueStarId];
                    }
                }
                return null;
            }

            uniqueStarId = UniqueStarIdWithGameName(gameName, clusterString, starId);
            if (gameNameSpecificConfig.ContainsKey(uniqueStarId))
            {
                return gameNameSpecificConfig[uniqueStarId];
            }

            if (!string.IsNullOrWhiteSpace(starName))
            {
                uniqueStarId = UniqueStarIdWithGameName(gameName, clusterString, starName);
                if (gameNameSpecificConfig.ContainsKey(uniqueStarId))
                {
                    return gameNameSpecificConfig[uniqueStarId];
                }
            }

            uniqueStarId = UniqueStarIdWithoutGameName(clusterString, starId);
            if (globalConfig.ContainsKey(uniqueStarId))
            {
                return globalConfig[uniqueStarId];
            }

            if (!string.IsNullOrWhiteSpace(starName))
            {
                uniqueStarId = UniqueStarIdWithoutGameName(clusterString, starName);
                if (globalConfig.ContainsKey(uniqueStarId))
                {
                    return globalConfig[uniqueStarId];
                }
            }
            return null;
        }
    }
}
