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
            context.PostAsync($"こんにちは！画像DE路線当てBot です。");
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // 変数定義
            string tag = "";     // 電車カテゴリータグ
            string msg = "";     // 返答メッセージ
            string lineName = "";   // [追加] 路線名 (駅すぱあと運航路線名)
            string lineCode = "";   // [追加] 路線コード (駅すぱあと運航路線コード)

            // Custom Vision API を使う準備
            var cvCred = new PredictionEndpointCredentials("YOUR_PREDICTION_KEY");
            var cvEp = new PredictionEndpoint(cvCred);
            var cvGuid = new Guid("YOUR_PROJECT_ID");

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

                    // タグを取得
                    tag = cvResult.Predictions[0].Tag;
                }
                catch
                {
                    // Error Handling
                }
            }

            // メッセージをセット
            if (tag != "")
            {
                //msg = tag + "、に いちばんにてるね！";
                // タグに応じてメッセージをセット
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
                var list = await GetStationList(lineCode); // ※GetStationList は次以降の項目で作成
                msg = lineName + "、に いちばんにてるね！\n\n"
                    + lineName + "は、以下のえきをはしるよ。\n\n---- - \n\n"
                    + list;
            }

            else
            {
                // 判定できなかった場合
                msg = "電車の写真を送ってね";
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
                        + "YOUR_ACCESSKEY"; //アクセスキー

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