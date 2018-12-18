using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using clk_rest.Models;
using clk_rest.Resources;

namespace clk_rest
{
    public class Service1 : IService1
    {
        private string db = Server.db;

        #region Profile management

        /// <summary>
        /// This will take an email and a password (hashed) and 
        /// create a new profile to the database.
        /// </summary>
        /// <param name="email">The email (used for login)</param>
        /// <param name="password">The password (in a hashed state)</param>
        /// <returns></returns>
        public int createProfile(Profile profile)
        {
            // Confirm that it is a valid email
            if (!Validators.isMail(profile.email))
                return -1;

            // Unique ID and timestamp
            string uid = Guid.NewGuid().ToString();
            string created = Time.timestamp();

            // TODO: Empty values? Check user doesn't already exist? Damn boii..

            // Create in database
            const string sql = "INSERT INTO profiles (email, password, ukey, created) VALUES (@email, @password, @ukey, @created)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand insert = new SqlCommand(sql, conn))
            {

                insert.Parameters.AddWithValue("@email", profile.email);
                insert.Parameters.AddWithValue("@password", profile.password);
                insert.Parameters.AddWithValue("@ukey", uid);
                insert.Parameters.AddWithValue("@created", created);

                conn.Open();
                int rowsAffected = insert.ExecuteNonQuery();

                conn.Close();
                return rowsAffected;
            }
        }

        /// <summary>
        /// Login method.
        /// Provide an email and password (in hashed state),
        /// if an row with matching information is found,
        /// the profile will be returned (i.e. the ID).
        /// </summary>
        /// <param name="email">The profile Email</param>
        /// <param name="hashedPassword">The profile password in a hashed state</param>
        /// <returns></returns>
        public Profile login(Profile profile)
        {
            Profile p = new Profile();
            if (profile.email == null || profile.password == null)
                return p;

            string sql = "SELECT * FROM profiles WHERE email=@email AND password=@password";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@email", profile.email);
                query.Parameters.AddWithValue("@password", profile.password);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return p;

                    result.Read();
                    p.created = result["created"].ToString();
                    p.email = result["email"].ToString();
                    p.id = result["ukey"].ToString();
                    p.username = result["username"].ToString();

                    conn.Close();
                    return p;
                }
            }
        }

        #endregion

        #region Public Service1 methods

        /// <summary>
        /// This will take a accept a board from the users local disk,
        /// and store it to the database. 
        /// </summary>
        /// <param name="board">The board object from the CLK application (BoardController from clk)</param>
        /// <returns>1 on success, -1 if it wasn't satisfied with its parameters</returns>
        public int saveBoard(Board board)
        {
            // Check the user
            Profile profile = new Profile();
            profile.id = board.userId;
            profile.password = board.password;

            if (!isUser(profile))
                return -1;

            // Make sure we aren't getting empty board values
            if (board.name.Equals("") || board.created.Equals("") || board.id.Equals(""))
                return -1;

            createBoard(board);
            createLists(board.lists);
            createCards(board.cards);
            createChecklist(board.checklists);
            createChecklistPoints(board.points);
            createComments(board.comments);

            // Add user to board connection in db
            createBoardMember(profile.id, board.id);

            return 1;
        }

        /// <summary>
        /// Get all boards associated to a profile.
        /// </summary>
        /// <param name="user">A valid profile</param>
        /// <returns>A list of all associated boards to that user</returns>
        public IList<Board> getBoards(Profile user)
        {
            IList<Board> boards = new List<Board>();

            // Confirm profile
            Profile p = login(user);
            if (p.id == null)
                return boards;

            string sql = "SELECT ukey, name FROM boards WHERE user_id=@uid";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@uid", p.id);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return boards;

                    while (result.Read())
                    {
                        boards.Add(new Board
                        {
                            name = result["name"].ToString(),
                            id = result["ukey"].ToString()
                        });
                    }

                    conn.Close();
                    return boards;
                }
            }
        }

        /// <summary>
        /// Get a specific board, with all its lists, cards etc.
        /// </summary>
        /// <param name="user">A valid user for the board</param>
        /// <param name="boardId">The UID for the board</param>
        /// <returns>(BoardController) Board with all its content</returns>
        public Board getBoard(Profile user, string boardId)
        {
            Board board = new Board();

            // Confirm user
            Profile p = login(user);
            if (p.id == null)
                return board;

            board = getBoard(boardId);
            IList<List> lists = getLists(boardId);
            IList<Card> cards = new List<Card>();
            IList<Checklist> checks = new List<Checklist>();
            IList<ChecklistPoint> points = new List<ChecklistPoint>();
            IList<Comment> comments = new List<Comment>();

            // Add cards
            foreach (List list in lists)
            {
                ((List<Card>)cards).AddRange(getCards(list.id));
            }

            // Add checklists & comments
            foreach (Card card in cards)
            {
                ((List<Checklist>)checks).AddRange(getChecklists(card.id));
                ((List<Comment>)comments).AddRange(getComments(card.id));
            }

            // Add checklist points
            foreach (Checklist check in checks)
            {
                ((List<ChecklistPoint>)points).AddRange(getPoints(check.id));
            }

            return board;
        }

        /// <summary>
        /// Deletes a profiles from the database.
        /// It will first make sure, the id/password match
        /// (make sure it is owner of profile, so not just everyone can delete profiles),
        /// then delete it from the database.
        /// It requires id and password of the profile.
        /// </summary>
        /// <param name="profile">The profile to delete</param>
        /// <returns>1 on success, -1 on fail</returns>
        public int removeProfile(Profile profile)
        {
            if (!isUser(profile))
                return -1;

            string sql = "DELETE FROM profiles WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@ukey", profile.id);
                conn.Open();
                query.ExecuteNonQuery();
                conn.Close();
            }

            return 1;
        }

        /// <summary>
        /// Public method to receive all members of a board (with user data).
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="boardId"></param>
        /// <returns></returns>
        public IList<BoardMember> getBoardMembers(Profile profile, string boardId)
        {
            IList<BoardMember> members = new List<BoardMember>();

            // Check user
            if (!isUser(profile))
                return members;

            // Confirm user is a member of board
            if (!isMember(profile, boardId))
                return members;

            return getMembersWithUserData(boardId);
        }

        /// <summary>
        /// Public method to add a profile as a member of a board.
        /// The profile parameter is the one submitting the new user to the board.
        /// This profile has to be a member of the board himself.
        /// </summary>
        /// <param name="profile">The requesting profile (an already member of the board)</param>
        /// <param name="boardId">The board to add the </param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public int addMemberToBoard(Profile profile, string boardId, string userId)
        {
            if (!isMember(profile, boardId))
                return -1;

            // TODO: Is the new user a match from the db?
            Profile p = new Profile();
            p.id = userId;

            createBoardMember(userId, boardId);

            return 1;
        }

        /// <summary>
        /// Public method to create a new list to the database.
        /// It will match that the user id is a member of the board.
        /// </summary>
        /// <param name="list">The List to create</param>
        /// <param name="userId">The ID of the user creating the new list.</param>
        /// <returns></returns>
        public int createList(List list, string userId)
        {
            Profile p = new Profile{id = userId};
            if (!isMember(p, list.boardId))
                return -1;

            List<List> pack = new List<List>();
            pack.Add(list);
            createLists(pack);

            return 1;
        }

        public int createCard(Card card, string userId)
        {

            return 1;
        }

        #endregion

        //TODO: Should I move all this below to a CRUD? I believe I should, yes. 
        #region Private GET List of elements

        /// <summary>
        /// This will receive all the user information for user associated to a board.
        /// It will first receive the list of board id => user id, then loop through
        /// those user records and fetch the user data for each user id.
        /// </summary>
        /// <param name="boardId">The ID of the board to receive member for</param>
        /// <returns>A List of BoardMember (email and username incl)</returns>
        private IList<BoardMember> getMembersWithUserData(string boardId)
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
        private IList<BoardMember> getMembers(string boardId)
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
        private string getBoardId(string cardId = "", 
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
        private string getCardId(string checklistId="", string commentId="")
        {
            string id = "";

            string table = "checklists";
            string matchId = checklistId;
            if (!commentId.Equals(""))
            {
                table = "comments";
                matchId = commentId;
            }

            string sql = "SELECT card_id FROM "+ table +" WHERE ukey=@ukey";
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
        private bool isUser(Profile profile)
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
        private bool isMember(Profile profile, string boardId)
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
        private Board getBoard(string boardId)
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
        private IList<List> getLists(string boardId)
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
        private IList<Card> getCards(string listId)
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
        private IList<Checklist> getChecklists(string cardId)
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
        private IList<ChecklistPoint> getPoints(string checklistId)
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
        private IList<Comment> getComments(string cardId)
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

        #endregion

        #region Private CREATE List of elements to the database

        /// <summary>
        /// Create a board to the database.
        /// Requires id, name, created and user id
        /// </summary>
        /// <param name="board">The board to create</param>
        private void createBoard(Board board)
        {
            // Make sure we aren't getting empty values.
            if (board.id == null
                || board.name == null
                || board.created == null
                || board.userId == null)
                return;

            string sql = "INSERT INTO boards (ukey, name, created, user_id) VALUES (@ukey, @name, @created, @user_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@ukey", board.id);
                query.Parameters.AddWithValue("@name", board.name);
                query.Parameters.AddWithValue("@created", board.created);
                query.Parameters.AddWithValue("@user_id", board.userId);

                conn.Open();
                query.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Create a list to the database.
        /// THis will loop through all indexes of a List,
        /// and create all the List to the database.
        /// Requires id, name, created and board id for each item.
        /// </summary>
        /// <param name="lists">A List of List</param>
        private void createLists(List<List> lists)
        {
            string sql = "INSERT INTO lists (ukey, name, created, board_id) VALUES (@ukey, @name, @created, @board_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (List list in lists)
                {
                    if (list.id == null
                        || list.name == null
                        || list.created == null
                        || list.boardId == null)
                        continue;

                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", list.id);
                    query.Parameters.AddWithValue("@name", list.name);
                    query.Parameters.AddWithValue("@created", list.created);
                    query.Parameters.AddWithValue("@board_id", list.boardId);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Creates a (or several) cards into the database.
        /// It takes a List of Card and create all the cards.
        /// It requires id, name, created, list id for each item.
        /// </summary>
        /// <param name="cards">A List of Card</param>
        private void createCards(List<Card> cards)
        {
            string sql = "INSERT INTO cards (ukey, name, created, list_id, description) VALUES (@ukey, @name, @created, @list_id, @description)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Card card in cards)
                {
                    if (card.id == null
                        || card.name == null
                        || card.created == null
                        || card.listId == null)
                        continue;

                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", card.id);
                    query.Parameters.AddWithValue("@name", card.name);
                    query.Parameters.AddWithValue("@created", card.created);
                    query.Parameters.AddWithValue("@list_id", card.listId);
                    query.Parameters.AddWithValue("@description", card.description);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }

        }

        /// <summary>
        /// Create checklists to the database.
        /// Takes a List of Checklist and will create all items.
        /// Requires id, name, created and card id for each item.
        /// </summary>
        /// <param name="checklists">A List of Checklist</param>
        private void createChecklist(List<Checklist> checklists)
        {
            string sql = "INSERT INTO checklists (ukey, name, created, card_id) VALUES (@ukey, @name, @created, @card_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Checklist ck in checklists)
                {
                    if (ck.id == null
                        || ck.name == null
                        || ck.created == null
                        || ck.cardId == null)
                        continue;

                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", ck.id);
                    query.Parameters.AddWithValue("@name", ck.name);
                    query.Parameters.AddWithValue("@created", ck.created);
                    query.Parameters.AddWithValue("@card_id", ck.cardId);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// This will create checklist points into the database.
        /// It will take a List of ChecklistPoint and create all the items.
        /// It requires id, name, created and checklist id for each item.
        /// </summary>
        /// <param name="points"></param>
        private void createChecklistPoints(List<ChecklistPoint> points)
        {
            string sql = "INSERT INTO checklist_points (ukey, description, created, checklist_id, checked) VALUES (@ukey, @name, @created, @checklist_id, @checked)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (ChecklistPoint point in points)
                {
                    if (point.id == null
                        || point.name == null
                        || point.created == null
                        || point.checklistId == null)
                        continue;

                    int isCheck = 0;
                    if (point.isCheck)
                        isCheck = 1;

                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", point.id);
                    query.Parameters.AddWithValue("@name", point.name);
                    query.Parameters.AddWithValue("@created", point.created);
                    query.Parameters.AddWithValue("@checklist_id", point.checklistId);
                    query.Parameters.AddWithValue("@checked", isCheck);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Create comments to the database.
        /// This will take a List of Comment and create each item.
        /// It requires id, comment, created, card id and user id for each item.
        /// </summary>
        /// <param name="comments">A List of Comment</param>
        private void createComments(List<Comment> comments)
        {
            string sql = "INSERT INTO comments (ukey, comment, created, card_id, user_id) VALUES (@ukey, @comment, @created, @card_id, @user_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Comment comment in comments)
                {
                    if (comment.id == null
                        || comment.comment == null
                        || comment.created == null
                        || comment.cardId == null
                        || comment.userId == null)
                        continue;

                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", comment.id);
                    query.Parameters.AddWithValue("@comment", comment.comment);
                    query.Parameters.AddWithValue("@created", comment.created);
                    query.Parameters.AddWithValue("@card_id", comment.cardId);
                    query.Parameters.AddWithValue("@user_id", comment.userId);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Create a member => board association in the database.
        /// </summary>
        /// <param name="profile">The profile to associate (requires id)</param>
        /// <param name="boardId">The board ID to associate the profile with</param>
        private void createBoardMember(string userId, string boardId)
        {

            string sql = "INSERT INTO board_members (board_id, user_id) VALUES (@boardId, @userId)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@boardId", boardId);
                query.Parameters.AddWithValue("@userId", userId);

                conn.Open();
                query.ExecuteNonQuery();
                conn.Close();
            }
        }

        #endregion
    }
}
