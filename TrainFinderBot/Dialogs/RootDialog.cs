using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Cognitive.CustomVision;
using System.Net.Http;
using Newtonsoft.Json;
using TrainFinderBot.Models;

namespace TrainFinderBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            // デフォルトのメッセージをセット
            context.PostAsync($"こんにちは！Train Finder Bot です。");

            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // 変数定義
            bool train = false;  // "train" タグの有無
            string tag = "";     // カテゴリータグ
            string lineName = "";   // 路線名 (駅すぱあと運航路線名)
            string lineCode = "";   // 路線名 (駅すぱあと運航路線コード)
            string list = "";   // 路線停車駅名リスト
            string msg = "";    // 返答メッセージ

            // Custom Vision API を使う準備
            var cvGuid = new Guid("d25f704c-4a54-4578-ac29-6b30ddcfa439");
            var cvCred = new PredictionEndpointCredentials("9c631fd082474bffba4b981ff91ab7c0");
            var cvEp = new PredictionEndpoint(cvCred);


            // 画像が送られてきたら Custom Vision を呼び出してタグを取得
            if (activity.Attachments?.Count != 0)
            {
                // 送られてきた画像を Stream として取得
                var photoUrl = activity.Attachments[0].ContentUrl;
                var client = new HttpClient();
                var photoStream = await client.GetStreamAsync(photoUrl);

                try
                {
                    // 画像を判定
                    var cvResult = await cvEp.PredictImageAsync(cvGuid, photoStream);

                    // train タグ および カテゴリーを取得
                    foreach (var item in cvResult.Predictions)
                    {
                        if (item.Probability > 0.5)
                        {
                            if (item.Tag == "Train")
                            {
                                train = true;
                            }
                            else
                            {
                                tag = item.Tag;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Error Handling
                }
            }

            // メッセージをセット
            if (tag != "")
            {
                // タグに応じて路線名をセット
                switch (tag)
                {
                    case "Chuo_Sobu":
                        lineName = "ＪＲ中央・総武線各駅停車";
                        lineCode = "110";
                        break;
                    case "Chuo_Ex":
                        lineName = "ＪＲ中央線快速";
                        lineCode = "109";
                        break;
                    case "Keihin-Tohoku":
                        lineName = "ＪＲ京浜東北線";
                        lineCode = "115";
                        break;
                    case "Tokaido":
                        lineName = "ＪＲ東海道本線";
                        lineCode = "117";
                        break;
                    case "Yamanote":
                        lineName = "ＪＲ山手線";
                        lineCode = "113";
                        break;
                    case "Yokosuka_SobuEx":
                        lineName = "ＪＲ横須賀線";
                        lineCode = "116";
                        break;
                }

                // 路線情報を取得してセット
                list = await GetStationList(lineCode);
                msg = lineName + "のようですね。\n\n 停車駅は \n\n---- - \n\n" + list + " \n\n---- - \n\n です。";

            }
            else if (train == true)
            {
                //msg = "I'm not sure what it is ...";
                msg = "この電車は分からないです．．．";
            }
            else
            {
                //msg = "Send me train photo!";
                msg = "電車の写真を送ってね♪";
            }
            await context.PostAsync(msg);

            context.Wait(MessageReceivedAsync);
        }

        private async Task<string> GetStationList(string lineCode)
        {
            var client = new HttpClient();

            // 路線名、アクセスキーをセット
            var ekiRequest = "http://api.ekispert.jp/v1/json/station?&operationLineCode="
                     + lineCode
                     + "&direction=down&key="
                     + "LE_zT88aARGDzx6n"; //アクセスキー

            // 路線情報の取得
            var ekiResult = await client.GetStringAsync(ekiRequest);
            var ekiStr = Uri.UnescapeDataString(ekiResult.ToString());
            var ekiModel = JsonConvert.DeserializeObject<StationModel>(ekiStr);

            // 停車駅情報を編集
            var stationList = "";
            foreach (var point in ekiModel.ResultSet.Point)
            {
                stationList = stationList + "→" + point.Station.Name;
            }
            stationList = stationList.Substring(1);

            return stationList;

        }
    }
}
