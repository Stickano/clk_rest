using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using clk_rest.Models;

namespace clk_rest
{
    [ServiceContract]
    public interface IService1
    {
        /// <summary>
        /// Create a profile.
        /// </summary>
        /// <param name="profile">The email and password (in hashed state) is required.</param>
        /// <returns>The newly created profile (contains UID)</returns>
        [WebInvoke(UriTemplate = "profile/create",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createProfile(Profile profile);

        /// <summary>
        /// Match login values (email & hashed password) to a profile in the database.
        /// </summary>
        /// <param name="profile">Email and Password (hashed) to match against in the database.</param>
        /// <returns>The profile of the user (UID included)</returns>
        [WebInvoke(UriTemplate = "profile/login",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        Profile login(Profile profile);

        /// <summary>
        /// Save a board to the database.
        /// </summary>
        /// <param name="board">(BoardController) Board, Lists, Cards, Checklists, Points, Comments, User ID and Password (hashed)</param>
        /// <returns>-1 on fail, 1 on success</returns> TODO: Should prolly give more informative feedback
        [WebInvoke(UriTemplate = "board/save",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int saveBoard(Board board);

        /// <summary>
        /// Get all boards associated with a user.
        /// </summary>
        /// <param name="user">The user to find associated boards for (UID and Password (hashed) required)</param>
        /// <returns>All boards associated with the profile</returns>
        [WebInvoke(UriTemplate = "board/getall",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        IList<Board> getBoards(Profile user);

        /// <summary>
        /// Get specific board from the database.
        /// </summary>
        /// <param name="user">The user that is fetching the board (UID and hashed password required)</param>
        /// <param name="boardId">The ID of the board to fetch</param>
        /// <returns>The board, full with lists, cards, checklists, points and comments</returns>
        [WebInvoke(UriTemplate = "board/get/{boardId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        Board getBoard(Profile user, string boardId);


        [WebInvoke(UriTemplate = "board/getmembers/{boardId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        IList<BoardMember> getBoardMembers(Profile user, string boardId);

        [WebInvoke(UriTemplate = "board/addmember/{boardId}/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int addMemberToBoard(Profile user, string boardId, string userId);

        [WebInvoke(UriTemplate = "board/createlist/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createList(List list, string userId);

        [WebInvoke(UriTemplate = "board/createcard/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createCard(Card card, string userId);

        [WebInvoke(UriTemplate = "board/createchecklist/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createChecklist(Checklist checklist, string userId);

        [WebInvoke(UriTemplate = "board/createpoint/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createChecklistPoint(ChecklistPoint point, string userId);

        [WebInvoke(UriTemplate = "board/createcomment/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createComment(Comment comment, string userId);

        [WebInvoke(UriTemplate = "board/updatelist/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int updateList(List list, string userId);

        [WebInvoke(UriTemplate = "board/updatecard/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int updateCard(Card card, string userId);

        [WebInvoke(UriTemplate = "board/updatechecklist/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int updateChecklist(Checklist checklist, string userId);

        [WebInvoke(UriTemplate = "board/updatepoint/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int updateChecklistPoint(ChecklistPoint point, string userId);

        [WebInvoke(UriTemplate = "board/updatecomment/{userId}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int updateComment(Comment comment, string userId);

        //[WebInvoke(UriTemplate = "board/getall",
        //    Method = "GET",
        //    ResponseFormat = WebMessageFormat.Json)]
        //[OperationContract]
    }
}
