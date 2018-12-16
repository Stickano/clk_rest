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
        /// This will take a accept a board from the users local disk,
        /// and store it to the database. 
        /// </summary>
        /// <param name="board">The board object from the CLK application (BoardController from clk)</param>
        /// <returns>1 on success, -1 if it wasn't satisfied with its parameters</returns>
        public int saveBoard(Board board)
        {

            // Confirm user
            string sql = "SELECT id FROM profiles WHERE ukey=@userId AND password=@password";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@userId", board.userId);
                query.Parameters.AddWithValue("@password", board.password);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    // If user was NOT found, return an empty token
                    if (!result.HasRows)
                        return -11;
                    conn.Close();
                }
            }

            // Make sure we aren't getting empty board values
            if (board.name.Equals("") || board.created.Equals("") || board.id.Equals(""))
                return -12;

            // Create the board
            sql = "INSERT INTO boards (ukey, name, created, user_id) VALUES (@ukey, @name, @created, @user_id)";
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

            // Create lists
            sql = "INSERT INTO lists (ukey, name, created, board_id) VALUES (@ukey, @name, @created, @board_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (List list in board.lists)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", list.id);
                    query.Parameters.AddWithValue("@name", list.name);
                    query.Parameters.AddWithValue("@created", list.created);
                    query.Parameters.AddWithValue("@board_id", board.id);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }

            // Create cards
            sql = "INSERT INTO cards (ukey, name, created, list_id, description) VALUES (@ukey, @name, @created, @list_id, @description)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Card card in board.cards)
                {
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

            // Create checklists
            sql = "INSERT INTO checklists (ukey, name, created, card_id) VALUES (@ukey, @name, @created, @card_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Checklist ck in board.checklists)
                {
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

            // Create checklist points (..doh  description = name)
            sql = "INSERT INTO checklist_points (ukey, description, created, checklist_id, checked) VALUES (@ukey, @name, @created, @checklist_id, @checked)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (ChecklistPoint point in board.points)
                {

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

            // Create comments
            sql = "INSERT INTO comments (ukey, comment, created, card_id, user_id) VALUES (@ukey, @comment, @created, @card_id, @user_id)";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Comment comment in board.comments)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@ukey", comment.id);
                    query.Parameters.AddWithValue("@comment", comment.comment);
                    query.Parameters.AddWithValue("@created", comment.created);
                    query.Parameters.AddWithValue("@card_id", comment.cardId);
                    query.Parameters.AddWithValue("@user_id", board.userId);

                    conn.Open();
                    query.ExecuteNonQuery();
                    conn.Close();
                }
            }

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
            Board b = new Board();

            // Confirm user
            Profile p = login(user);
            if (p.id == null)
                return b;

            // Get board
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
                        return b;

                    result.Read();
                    b.created = result["created"].ToString();
                    b.name = result["name"].ToString();
                    b.id = result["ukey"].ToString();

                    conn.Close();
                }
            }

            // Get lists
            sql = "SELECT * FROM lists WHERE board_id=@boardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@boardId", boardId);
                conn.Open();
                using (SqlDataReader result = query.ExecuteReader())
                {
                    while (result.Read())
                    {
                        b.lists.Add(new List
                        {
                            boardId = result["board_id"].ToString(),
                            id = result["ukey"].ToString(),
                            name = result["name"].ToString(),
                            created = result["created"].ToString()
                        });
                    }
                    
                    conn.Close();
                }
            }

            // Get cards
            sql = "SELECT * FROM cards WHERE list_id=@listId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (List list in b.lists)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@listId", list.id);
                    conn.Open();
                    using (SqlDataReader result = query.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            b.cards.Add(new Card
                            {
                                listId = result["list_id"].ToString(),
                                id = result["ukey"].ToString(),
                                name = result["description"].ToString(),
                                created = result["created"].ToString()
                            });
                        }

                        conn.Close();
                    }
                }
                
            }

            // Get checklists
            sql = "SELECT * FROM checklists WHERE card_id=@cardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Card card in b.cards)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@cardId", card.id);
                    conn.Open();
                    using (SqlDataReader result = query.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            b.checklists.Add(new Checklist
                            {
                                cardId = result["card_id"].ToString(),
                                id = result["ukey"].ToString(),
                                name = result["name"].ToString(),
                                created = result["created"].ToString()
                            });
                        }

                        conn.Close();
                    }
                }

            }

            // Get checklist points
            sql = "SELECT * FROM checklist_points WHERE checklist_id=@checkId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Checklist check in b.checklists)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@checkId", check.id);
                    conn.Open();
                    using (SqlDataReader result = query.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            bool isCheck = result["checked"].ToString().Equals("1");

                            b.points.Add(new ChecklistPoint
                            {
                                checklistId = result["checklist_id"].ToString(),
                                id = result["ukey"].ToString(),
                                name = result["description"].ToString(),
                                created = result["created"].ToString(),
                                isCheck = isCheck
                            });
                        }

                        conn.Close();
                    }
                }

            }

            // Get comments
            sql = "SELECT * FROM comments WHERE card_id=@cardId AND active=1";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                foreach (Card card in b.cards)
                {
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@cardId", card.id);
                    conn.Open();
                    using (SqlDataReader result = query.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            b.comments.Add(new Comment
                            {
                                cardId = result["card_id"].ToString(),
                                id = result["ukey"].ToString(),
                                comment = result["comment"].ToString(),
                                created = result["created"].ToString(),
                                userId = result["user_id"].ToString()
                            });
                        }

                        conn.Close();
                    }
                }

                return b;
            }
        }
    }
}
