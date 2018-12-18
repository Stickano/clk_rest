using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using clk_rest.Models;

namespace clk_rest.CRUD
{
    public class Create
    {
        private string db;

        public Create(string db)
        {
            this.db = db;
        }

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

    }
}