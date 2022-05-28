using ComputerUtils.Encryption;
using ComputerUtils.Logging;
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

        public static List<Message> GetMessages(long channelId, int before = -1, int after = -1, int count = 100)
        {
            IMongoCollection<Message> channel = zwietrachtDatabase.GetCollection<Message>(channelId.ToString());
            if(channel == null) return null;
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
            foreach(User u in channel.participants)
            {
                User found = GetUser(u.nickname);
                if (found != null)
                {
                    participants.Add(found);
                }
            }
            BsonArray users = new BsonArray();
            foreach(User u in participants)
            {
                users.Add(new BsonDocument("$contains"))
            }
            // ToDo: Add check to check if user is in participants
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
            message.id = DateTime.Now.Ticks.ToString();
            zwietrachtDatabase.GetCollection<Message>(channelId).InsertOne(message);
            //message.author.nickname = user.nickname; // makes sure users use the right username to send messages. But it's more fun without validation.
        }
    }
}
