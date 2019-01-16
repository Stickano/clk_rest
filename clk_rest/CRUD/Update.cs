using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using clk_rest.Models;

namespace clk_rest.CRUD
{
    public class Update
    {
        private string db;

        public Update(string db)
        {
            this.db = db;
        }

        /// <summary>
        /// Update a List in the database
        /// </summary>
        /// <param name="list">The List to update</param>
        /// <returns>The amount of rows affected</returns>
        public int updateList(List list)
        {
            if (list.name == null || list.id == null)
                return -1;

            int active = 1;
            if (!list.active)
                active = 0;

            string sql = "UPDATE lists SET name=@name, active=" + active + " WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@name", list.name);
                query.Parameters.AddWithValue("@ukey", list.id);

                conn.Open();
                int rowsAffected = query.ExecuteNonQuery();
                conn.Close();

                return rowsAffected;
            }
        }

        /// <summary>
        /// Update a Card in the database
        /// </summary>
        /// <param name="card">The card to update</param>
        /// <returns>The amount of rows affected</returns>
        public int updateCard(Card card)
        {
            if (card.name == null || card.listId == null || card.id == null)
                return -1;

            int active = 1;
            if (!card.active)
                active = 0;

            string description = "";
            if (card.description != null)
                description = card.description;

            string sql = "UPDATE cards SET name=@name, list_id=@list_id, description=@description, active=" + active + " WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@name", card.name);
                query.Parameters.AddWithValue("@ukey", card.id);
                query.Parameters.AddWithValue("@list_id", card.listId);
                query.Parameters.AddWithValue("@description", description);

                conn.Open();
                int rowsAffected = query.ExecuteNonQuery();
                conn.Close();

                return rowsAffected;
            }
        }

        /// <summary>
        /// Update a Checklist in the database.
        /// </summary>
        /// <param name="checklist">The Checklist to update</param>
        /// <returns>The amount of rows affected</returns>
        public int updateChecklist(Checklist checklist)
        {
            if (checklist.name == null || checklist.id == null || checklist.cardId == null)
                return -1;

            int active = 1;
            if (!checklist.active)
                active = 0;

            string sql = "UPDATE checklists SET name=@name, card_id=@card_id, active=" + active + " WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@name", checklist.name);
                query.Parameters.AddWithValue("@ukey", checklist.id);
                query.Parameters.AddWithValue("@card_id", checklist.cardId);

                conn.Open();
                int rowsAffected = query.ExecuteNonQuery();
                conn.Close();

                return rowsAffected;
            }
        }

        /// <summary>
        /// Update a Checklist Point in the database
        /// </summary>
        /// <param name="point">The point to update</param>
        /// <returns>The amount of rows affected</returns>
        public int updateChecklistPoint(ChecklistPoint point)
        {
            if (point.name == null || point.id == null || point.checklistId == null)
                return -1;

            int active = 1;
            if (!point.active)
                active = 0;

            int isCheck = 0;
            if (point.isCheck)
                isCheck = 1;
            

            string sql = "UPDATE checklist_points SET description=@description, checklist_id=@checklist_id, checked="+ isCheck +", active=" + active + " WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@description", point.name);
                query.Parameters.AddWithValue("@ukey", point.id);
                query.Parameters.AddWithValue("@checklist_id", point.checklistId);

                conn.Open();
                int rowsAffected = query.ExecuteNonQuery();
                conn.Close();

                return rowsAffected;
            }
        }

        /// <summary>
        /// Update an Comment in the database
        /// </summary>
        /// <param name="comment">The Comment to update</param>
        /// <returns>The amount of rows affected</returns>
        public int updateComment(Comment comment)
        {
            if (comment.comment == null
                || comment.cardId == null
                || comment.id == null
                || comment.userId == null)
                return -1;

            int active = 1;
            if (!comment.active)
                active = 0;

            string sql = "UPDATE comments SET comment=@comment, card_id=@card_id, active=" + active + " WHERE ukey=@ukey";
            using (SqlConnection conn = new SqlConnection(db))
            using (SqlCommand query = new SqlCommand(sql, conn))
            {
                query.Parameters.AddWithValue("@comment", comment.comment);
                query.Parameters.AddWithValue("@ukey", comment.id);
                query.Parameters.AddWithValue("@card_id", comment.cardId);

                conn.Open();
                int rowsAffected = query.ExecuteNonQuery();
                conn.Close();

                return rowsAffected;
            }
        }
    }
}