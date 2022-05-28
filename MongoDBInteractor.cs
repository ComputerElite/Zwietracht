using ComputerUtils.Encryption;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Zwietracht.Models;

namespace Zwietracht
{
    public class MongoDBInteractor
    {
        public static MongoClient mongoClient = null;
        public static IMongoDatabase zwietrachtDatabase = null;
        public static IMongoCollection<UserInfo> userCollection = null;
        public static IMongoCollection<Channel> channelCollection = null;
        public static Config config
        { 
            get
            {
                return ZwietrachtEnvironment.config;
            }
        }

        public static void Init()
        {
            RemoveIdRemap<User>();
            RemoveIdRemap<UserInfo>();
            RemoveIdRemap<Channel>();
            RemoveIdRemap<Message>();

            mongoClient = new MongoClient(ZwietrachtEnvironment.config.mongoDBUrl);
            zwietrachtDatabase = mongoClient.GetDatabase(ZwietrachtEnvironment.config.mongoDBName);
            userCollection = zwietrachtDatabase.GetCollection<UserInfo>("users");
            channelCollection = zwietrachtDatabase.GetCollection<Channel>("channels");
        }

        public static List<Message> GetMessages(string channelId, int before = -1, int after = -1, int count = 100, string token = "")
        {
            Channel c = GetChannel(channelId);
            if (c == null) return new List<Message>();
            User me = GetUserByToken(token);
            if(me == null) return new List<Message>();
            if (c.participants.Where(x => x.id == me.id).Count() < 1) return new List<Message>();
            IMongoCollection<Message> channel = zwietrachtDatabase.GetCollection<Message>(channelId.ToString());
            if (channel == null) return new List<Message>();
            if (before == -1 && after == -1)
            {
                return channel.Find(x => true).SortByDescending(x => x.id).Limit(count).ToList();
            }
            else if (before == -1 && after != -1)
            {
                return channel.Find(x => Convert.ToInt64(x.id) < before).SortByDescending(x => x.id).Limit(count).ToList();
            }
            else if (before != -1 && after == -1)
            {
                return channel.Find(x => Convert.ToInt64(x.id) > after).SortBy(x => x.id).Limit(count).ToList();
            }
            return null;
        }

        public static Channel GetChannel(string channelId)
        {
            Logger.Log(channelCollection.Find(x => x.id == channelId).Any().ToString());
            return channelCollection.Find(x => x.id == channelId).FirstOrDefault();
        }

        public static bool DoesUserExist(string username)
        {
            return userCollection.Find(x => x.nickname == username).Any();
        }

        public static UserInfo GetUserInfo(string nickname)
        {
            return userCollection.Find(x => x.nickname == nickname).FirstOrDefault();
        }

        public static User GetUser(string nickname)
        {
            return ObjectConverter.ConvertCopy<User, UserInfo>(userCollection.Find(x => x.nickname == nickname).FirstOrDefault());
        }

        public static void UpdateUser(UserInfo u)
        {
            userCollection.ReplaceOne(x => x.id == u.id, u);
        }

        public static UserInfo GetUserInfoById(string id)
        {
            return userCollection.Find(x => x.id == id).FirstOrDefault();
        }

        public static User GetUserByToken(string token)
        {
            string sha256 = Hasher.GetSHA256OfString(token);
            return ObjectConverter.ConvertCopy<User, UserInfo>(userCollection.Find(x => x.currentTokenSHA256 == sha256).FirstOrDefault());
        }

        public static void AddUser(UserInfo user)
        {
            userCollection.InsertOne(user);
        }

        public static void RemoveIdRemap<T>()
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                if (typeof(T).GetMember("id").Length > 0)
                {
                    Logger.Log("Unmapping reassignment for " + typeof(T).Name + " id -> _id");
                    cm.UnmapProperty("id");
                    cm.MapMember(typeof(T).GetMember("id")[0])
                        .SetElementName("id")
                        .SetOrder(0) //specific to your needs
                        .SetIsRequired(true); // again specific to your needs
                }
            });
        }

        public static void CreateChannel(Channel channel, string token)
        {
            List<User> participants = new List<User>();
            User me = GetUserByToken(token);
            foreach(User u in channel.participants)
            {
                User found = GetUser(u.nickname);
                if (found != null && !participants.Where(x => x.id == found.id).Any())
                {
                    participants.Add(found);
                }
            }
            BsonArray users = new BsonArray();
            bool userThere = false;
            foreach(User u in participants)
            {
                if(u.id == me.id) userThere = true;
                users.Add(new BsonDocument("participants", new BsonDocument("$elemMatch", new BsonDocument("id", u.id))));
            }
            if(!userThere)
            {
                return; // User who created the channel isn't participating
            }
            if(channelCollection.Find(new BsonDocument("$and", users)).Any())
            {
                return; //channel already exists
            }
            // Add channel
            channelCollection.InsertOne(new Channel { participants = participants });
        }

        public static User Me(string token)
        {
            User u = GetUserByToken(token);
            if (u == null) return new User();
            u.channels = channelCollection.Find(x => x.participants.Contains(u)).ToList();
            return u;
        }

        public static void AddMessage(Message message, string token, string channelId)
        {
            UserInfo user = GetUserInfoById(message.author.id);
            if (user == null || user.currentTokenSHA256 != Hasher.GetSHA256OfString(token)) return;
            Channel c = GetChannel(channelId);
            if (c == null) return;
            message.id = DateTime.Now.Ticks.ToString();
            for (int i = 0; i < message.attachments.Count; i++)
            {
                string path = channelId + Path.DirectorySeparatorChar + message.id + Path.DirectorySeparatorChar + i;
                string type = SaveBase64Url(message.attachments[i].base64, ZwietrachtEnvironment.dataDir + "files" + Path.DirectorySeparatorChar + path);
                path += type;
                path = "/cdn/" + path;
                message.attachments[i].base64 = "";
                message.attachments[i].relUrl = path;
                message.attachments[i].url = config.publicAddress + path.Substring(1);
            }
            zwietrachtDatabase.GetCollection<Message>(channelId).InsertOne(message);
            //message.author.nickname = user.nickname; // makes sure users use the right username to send messages. But it's more fun without validation.
        }

        public static string SaveBase64Url(string url, string pathWithoutFileType)
        {
            string fileType = HttpServer.GetFileExtension(url.Substring(url.IndexOf(":") + 1, url.IndexOf(";") - url.IndexOf(":") - 1));
            FileManager.CreateDirectoryIfNotExisting(Path.GetDirectoryName(pathWithoutFileType));
            File.WriteAllBytes(pathWithoutFileType + fileType, Convert.FromBase64String(url.Substring(url.IndexOf(",") + 1)));
            return fileType;
        }
    }
}
