using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using clk_rest.CRUD;
using clk_rest.Models;
using clk_rest.Resources;

namespace clk_rest
{
    public class Service1 : IService1
    {
        private static string db = Server.db;
        Create create = new Create(db);
        Read read = new Read(db);
        Update update = new Update(db);

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

            if (profile.password == null)
                return -1;

            // Unique ID and timestamp
            string uid = Guid.NewGuid().ToString();
            string created = Time.timestamp();

            // TODO: Does user already exists?
            
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
            if (!read.isUser(profile))
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

            if (!read.isUser(profile))
                return -1;

            // Make sure we aren't getting empty board values
            if (board.name.Equals("") || board.created.Equals("") || board.id.Equals(""))
                return -1;

            create.createBoard(board);
            create.createLists(board.lists);
            create.createCards(board.cards);
            create.createChecklists(board.checklists);
            create.createChecklistPoints(board.points);
            create.createComments(board.comments);

            // Add user to board connection in db
            create.createBoardMember(profile.id, board.id);

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

            //TODO: You are not fetching members of boards table.. Idiot.!?

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

            board = read.getBoard(boardId);
            IList<List> lists = read.getLists(boardId);
            IList<Card> cards = new List<Card>();
            IList<Checklist> checks = new List<Checklist>();
            IList<ChecklistPoint> points = new List<ChecklistPoint>();
            IList<Comment> comments = new List<Comment>();

            // Add cards
            foreach (List list in lists)
            {
                ((List<Card>)cards).AddRange(read.getCards(list.id));
            }

            // Add checklists & comments
            foreach (Card card in cards)
            {
                ((List<Checklist>)checks).AddRange(read.getChecklists(card.id));
                ((List<Comment>)comments).AddRange(read.getComments(card.id));
            }

            // Add checklist points
            foreach (Checklist check in checks)
            {
                ((List<ChecklistPoint>)points).AddRange(read.getPoints(check.id));
            }

            return board;
        }

        /// <summary>
        /// Public method to receive all members of a board (with user data).
        /// </summary>
        /// <param name="profile">The user that is making the request</param>
        /// <param name="boardId">The ID of the board to fetch members for</param>
        /// <returns>A list of BoardMember (user id and board id)</returns>
        public IList<BoardMember> getBoardMembers(Profile profile, string boardId)
        {
            IList<BoardMember> members = new List<BoardMember>();

            // Check user
            if (!read.isUser(profile))
                return members;

            // Confirm user is a member of board
            if (!read.isMember(profile, boardId))
                return members;

            return read.getMembersWithUserData(boardId);
        }

        /// <summary>
        /// Public method to add a profile as a member of a board.
        /// The profile parameter is the one submitting the new user to the board.
        /// This profile has to be a member of the board himself.
        /// </summary>
        /// <param name="profile">The requesting profile (an already member of the board)</param>
        /// <param name="boardId">The board to add the </param>
        /// <param name="userId">The ID of the user to add to the board</param>
        /// <returns>1 on success, -1 on fail</returns>
        public int addMemberToBoard(Profile profile, string boardId, string email)
        {
            if (!read.isMember(profile, boardId))
                return -1;

            string userId = read.isProfile(email);
            if (userId.Equals(""))
                return -1;

            create.createBoardMember(userId, boardId);
            return 1;
        }

        /// <summary>
        /// Public method to create a new list to the database.
        /// It will match that the user id is a member of the board.
        /// </summary>
        /// <param name="list">The List to create</param>
        /// <param name="userId">The ID of the user creating the new list.</param>
        /// <returns>1 on success, -1 on fail</returns>
        public int createList(List list, string userId)
        {
            Profile p = new Profile{id = userId};
            if (!read.isMember(p, list.boardId))
                return -1;

            List<List> pack = new List<List>();
            pack.Add(list);
            create.createLists(pack);

            return 1;
        }

        /// <summary>
        /// Public method to create a new card in the database
        /// It will make sure that the user is a member of the board.
        /// </summary>
        /// <param name="card">The card to create</param>
        /// <param name="userId">The user ID who is creating the card</param>
        /// <returns>1 on success, -1 on fail</returns>
        public int createCard(Card card, string userId)
        {
            string boardId = read.getBoardId(card.id);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            List<Card> pack = new List<Card>();
            pack.Add(card);
            create.createCards(pack);

            return 1;
        }

        /// <summary>
        /// Public method to create a new checklist to the database.
        /// User will be matched, that it is a member of the board.
        /// </summary>
        /// <param name="checklist">The checklist to create</param>
        /// <param name="userId">The user ID who is creating the checklist</param>
        /// <returns>1 on success, -1 on fail</returns>
        public int createChecklist(Checklist checklist, string userId)
        {
            string boardId = read.getBoardId(checklist.cardId);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            List<Checklist> pack = new List<Checklist>();
            pack.Add(checklist);
            create.createChecklists(pack);

            return 1;
        }

        /// <summary>
        /// Public method to create a checklist point to the database.
        /// It will confirm that user is a member of the board.
        /// </summary>
        /// <param name="point">The checklist point to create</param>
        /// <param name="userId">The user ID who is creating the point</param>
        /// <returns>1 on success, -1 on fail</returns>
        public int createChecklistPoint(ChecklistPoint point, string userId)
        {
            string boardId = read.getBoardId("", point.checklistId);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            List<ChecklistPoint> pack = new List<ChecklistPoint>();
            pack.Add(point);
            create.createChecklistPoints(pack);

            return 1;
        }

        /// <summary>
        /// Public method to create a comment to a card in the database.
        /// The user will be confirmed to be a member of the board.
        /// </summary>
        /// <param name="comment">The comment to create</param>
        /// <param name="userId">The ID of the user making the request</param>
        /// <returns></returns>
        public int createComment(Comment comment, string userId)
        {
            string boardId = read.getBoardId(comment.cardId);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            List<Comment> pack = new List<Comment>();
            pack.Add(comment);
            create.createComments(pack);

            return 1;
        }

        /// <summary>
        /// Public method to update a list in the database
        /// </summary>
        /// <param name="list">The List to update</param>
        /// <param name="userId">The ID of the user making the request</param>
        /// <returns></returns>
        public int updateList(List list, string userId)
        {
            string boardId = list.boardId;

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            return update.updateList(list);
        }

        /// <summary>
        /// Public method to update a card in the database.
        /// </summary>
        /// <param name="card">The card to update</param>
        /// <param name="userId">The ID of the user making the request</param>
        /// <returns></returns>
        public int updateCard(Card card, string userId)
        {
            string boardId = read.getBoardId(card.id);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            return update.updateCard(card);
        }

        /// <summary>
        /// Public method to update a checklist in the database
        /// </summary>
        /// <param name="checklist">The checklist to update</param>
        /// <param name="userId">The ID of the user making the request</param>
        /// <returns></returns>
        public int updateChecklist(Checklist checklist, string userId)
        {
            string boardId = read.getBoardId(checklist.cardId);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            return update.updateChecklist(checklist);
        }

        /// <summary>
        /// Public method to update a checklist point in the database
        /// </summary>
        /// <param name="point">The point to update</param>
        /// <param name="userId">The ID of the user making the request</param>
        /// <returns></returns>
        public int updateChecklistPoint(ChecklistPoint point, string userId)
        {
            string boardId = read.getBoardId("", point.checklistId);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            return update.updateChecklistPoint(point);
        }

        /// <summary>
        /// Public method to update a comment in the database
        /// </summary>
        /// <param name="comment">The comment to update</param>
        /// <param name="userId">The ID of the user making the request</param>
        /// <returns>The number of rows affected</returns>
        public int updateComment(Comment comment, string userId)
        {
            string boardId = read.getBoardId(comment.cardId);

            Profile p = new Profile { id = userId };
            if (!read.isMember(p, boardId))
                return -1;

            return update.updateComment(comment);
        }

        #endregion

    }
}
