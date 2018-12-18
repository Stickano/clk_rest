using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using clk_rest.Models;

namespace clk_rest.CRUD
{
    public class Read
    {
        private string db;

        public Read(string db)
        {
            this.db = db;
        }

        /// <summary>
        /// This will receive all the user information for user associated to a board.
        /// It will first receive the list of board id => user id, then loop through
        /// those user records and fetch the user data for each user id.
        /// </summary>
        /// <param name="boardId">The ID of the board to receive member for</param>
        /// <returns>A List of BoardMember (email and username incl)</returns>
        public IList<BoardMember> getMembersWithUserData(string boardId)
        {
            IList<BoardMember> members = getMembers(boardId);

            string sql = "SELECT * FROM profiles WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (BoardMember member in members)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", member.userId);
                    conn.Open();
                    using (SqlDataReader result = query.ExecuteReader())
                    {
                        result.Read();
                        member.email = result["email"].ToString();
                        member.username = result["username"].ToString();
                        conn.Close();
                    }
                }

                return members;
            }
        }

        /// <summary>
        /// This will receive all members (user ids) of a board.
        /// </summary>
        /// <param name="boardId">The ID of the board to receive members for</param>
        /// <returns>A List of BoardMember (board id, user id)</returns>
        public IList<BoardMember> getMembers(string boardId)
        {
            IList<BoardMember> members = new List<BoardMember>();

            string sql = "SELECT * FROM board_members WHERE board_id=@boardId";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@boardId", boardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        BoardMember member = new BoardMember();
                        member.boardId = boardId;
                        member.userId = result["user_id"].ToString();
                    }

                    conn.Close();
                    return members;
                }
            }
        }

        /// <summary>
        /// Get a Board ID from any element (card, checklist, point or comment).
        /// This is a parent method for the below couple of methods.
        /// </summary>
        /// <param name="cardId">Find Board ID from Card ID</param>
        /// <param name="checkId">Find Board ID from Checklist ID</param>
        /// <param name="pointId">Find Board ID from CHecklist Point ID</param>
        /// <param name="commentId">Find Board ID from Comment ID</param>
        /// <returns>The Board ID</returns>
        public string getBoardId(string cardId = "",
                                  string checkId = "",
                                  string pointId = "",
                                  string commentId = "")
        {

            string boardId = "";

            string lid = "";    // list id
            string cid = "";    // card id
            string chid = "";   // check id

            // Find from card id 
            if (!cardId.Equals(""))
            {
                lid = getListId(cardId);
                boardId = getBoardId(lid);
            }

            // Find from checklist id
            if (!checkId.Equals(""))
            {
                cid = getCardId(checkId);
                lid = getListId(cid);
                boardId = getBoardId(lid);
            }

            // Find from comment id
            if (!commentId.Equals(""))
            {
                cid = getCardId("", commentId);
                lid = getListId(cid);
                boardId = getBoardId(lid);
            }

            // Find from checklist point id
            if (!pointId.Equals(""))
            {
                chid = getChecklistId(pointId);
                cid = getCardId(chid);
                lid = getListId(cid);
                boardId = getBoardId(lid);
            }

            return boardId;
        }

        /// <summary>
        /// Get the Board ID from a List ID
        /// </summary>
        /// <param name="listId">The List ID to find a Board ID from</param>
        /// <returns>The Board ID for the List</returns>
        private string getBoardId(string listId)
        {
            string id = "";

            string sql = "SELECT board_id FROM lists WHERE ukey=@listId";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@listId", listId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return id;

                    result.Read();
                    id = result["board_id"].ToString();

                    conn.Close();
                    return id;
                }
            }
        }

        /// <summary>
        /// Get the List ID from a Card ID
        /// </summary>
        /// <param name="cardId">The Card ID to find a List ID from</param>
        /// <returns>The List ID for the Card</returns>
        private string getListId(string cardId)
        {
            string id = "";

            string sql = "SELECT list_id FROM cards WHERE ukey=@cardId";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@cardId", cardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return id;

                    result.Read();
                    id = result["list_id"].ToString();

                    conn.Close();
                    return id;
                }
            }
        }

        /// <summary>
        /// Get the Card ID from either a Checklist ID, or a Comment ID.
        /// </summary>
        /// <param name="checklistId">Incl. to find Card ID from a Checklist ID</param>
        /// <param name="commentId">Incl. to find a Card ID from a Comment ID</param>
        /// <returns>The Card ID of either the Checklist or the Comment</returns>
        private string getCardId(string checklistId = "", string commentId = "")
        {
            string id = "";

            string table = "checklists";
            string matchId = checklistId;
            if (!commentId.Equals(""))
            {
                table = "comments";
                matchId = commentId;
            }

            string sql = "SELECT card_id FROM " + table + " WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@ukey", matchId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return id;

                    result.Read();
                    id = result["card_id"].ToString();

                    conn.Close();
                    return id;
                }
            }
        }

        /// <summary>
        /// Get the Checklist ID from a ChecklistPoint ID
        /// </summary>
        /// <param name="pointId">The Checklist Point ID</param>
        /// <returns>The Checklist ID for the Checklist Point</returns>
        private string getChecklistId(string pointId)
        {
            string id = "";

            string sql = "SELECT checklist_id FROM checklist_points WHERE ukey=@pointId";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@pointId", pointId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return id;

                    result.Read();
                    id = result["checklist_id"].ToString();

                    conn.Close();
                    return id;
                }
            }
        }

        /// <summary>
        /// This will match a Profile against a user in the database.
        /// Will match against user id and password
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public bool isUser(Profile profile)
        {
            if (profile.id == null || profile.password == null)
                return false;

            // Confirm user
            string sql = "SELECT id FROM profiles WHERE ukey=@userId AND password=@password";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@userId", profile.id);
                query.Parameters.AddWithValue("@password", profile.password);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                    {
                        conn.Close();
                        return false;
                    }
                    conn.Close();
                    return true;
                }
            }
        }

        /// <summary>
        /// This will check if a profile is a member of a board.
        /// </summary>
        /// <param name="profile">The profile to match</param>
        /// <param name="boardId">The board ID to check against</param>
        /// <returns>True/False if member or not</returns>
        public bool isMember(Profile profile, string boardId)
        {
            if (profile.id == null)
                return false;

            string sql = "SELECT id FROM board_members WHERE user_id=@userId AND board_id=@boardId";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@userId", profile.id);
                query.Parameters.AddWithValue("@boardId", boardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    if (!result.HasRows)
                    {
                        conn.Close();
                        return false;
                    }
                    conn.Close();
                    return true;
                }
            }
        }

        /// <summary>
        /// Receive the data of a specific board in the database.
        /// </summary>
        /// <param name="boardId">The ID of the board to receive</param>
        /// <returns>A Board object with all its content (lists, cards, checklists etc)</returns>
        public Board getBoard(string boardId)
        {
            Board board = new Board();

            string sql = "SELECT * FROM boards WHERE ukey=@boardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@boardId", boardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return board;

                    result.Read();
                    board.created = result["created"].ToString();
                    board.name = result["name"].ToString();
                    board.id = result["ukey"].ToString();

                    conn.Close();
                    return board;
                }
            }
        }

        /// <summary>
        /// Receive all lists associated to board
        /// </summary>
        /// <param name="boardId">The ID of the board to receive lists for</param>
        /// <returns>A List of List</returns>
        public IList<List> getLists(string boardId)
        {
            IList<List> lists = new List<List>();

            string sql = "SELECT * FROM lists WHERE board_id=@boardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@boardId", boardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        List list = new List();
                        list.boardId = result["board_id"].ToString();
                        list.id = result["ukey"].ToString();
                        list.name = result["name"].ToString();
                        list.created = result["created"].ToString();
                        lists.Add(list);
                    }

                    conn.Close();
                    return lists;
                }
            }
        }

        /// <summary>
        /// Receive all the cards associated to a list
        /// </summary>
        /// <param name="listId">The ID of the list to receive for</param>
        /// <returns>A List of Card</returns>
        public IList<Card> getCards(string listId)
        {
            IList<Card> cards = new List<Card>();

            string sql = "SELECT * FROM cards WHERE list_id=@listId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@listId", listId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        Card card = new Card();
                        card.listId = result["list_id"].ToString();
                        card.id = result["ukey"].ToString();
                        card.name = result["description"].ToString();
                        card.created = result["created"].ToString();
                        cards.Add(card);
                    }

                    conn.Close();
                    return cards;
                }
            }
        }

        /// <summary>
        /// Receive all the checklists associated to a card
        /// </summary>
        /// <param name="cardId">The ID of the card to receive for</param>
        /// <returns>A List of Checklist</returns>
        public IList<Checklist> getChecklists(string cardId)
        {
            IList<Checklist> checks = new List<Checklist>();

            string sql = "SELECT * FROM checklists WHERE card_id=@cardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.Clear();
                query.Parameters.AddWithValue("@cardId", cardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        Checklist check = new Checklist();
                        check.cardId = result["card_id"].ToString();
                        check.id = result["ukey"].ToString();
                        check.name = result["name"].ToString();
                        check.created = result["created"].ToString();

                        checks.Add(check);
                    }

                    conn.Close();
                    return checks;
                }
            }
        }

        /// <summary>
        /// Receive all the checklist points associated with a checklist
        /// </summary>
        /// <param name="checklistId">The ID of the checklist to receive for</param>
        /// <returns>A List of ChecklistPoint</returns>
        public IList<ChecklistPoint> getPoints(string checklistId)
        {
            IList<ChecklistPoint> points = new List<ChecklistPoint>();

            string sql = "SELECT * FROM checklist_points WHERE checklist_id=@checkId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@checkId", checklistId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        ChecklistPoint point = new ChecklistPoint();
                        bool isCheck = result["checked"].ToString().Equals("1");
                        point.checklistId = result["checklist_id"].ToString();
                        point.id = result["ukey"].ToString();
                        point.name = result["description"].ToString();
                        point.created = result["created"].ToString();
                        point.isCheck = isCheck;

                        points.Add(point);
                    }

                    conn.Close();
                    return points;
                }
            }
        }

        /// <summary>
        /// Receive all the comments associated to a card
        /// </summary>
        /// <param name="cardId">The ID of the card to receive for</param>
        /// <returns>A List of Comment</returns>
        public IList<Comment> getComments(string cardId)
        {
            IList<Comment> comments = new List<Comment>();

            string sql = "SELECT * FROM comments WHERE card_id=@cardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@cardId", cardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        Comment comment = new Comment();
                        comment.cardId = result["card_id"].ToString();
                        comment.id = result["ukey"].ToString();
                        comment.comment = result["comment"].ToString();
                        comment.created = result["created"].ToString();
                        comment.userId = result["user_id"].ToString();

                        comments.Add(comment);
                    }

                    conn.Close();
                    return comments;
                }
            }
        }

    }
}