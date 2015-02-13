///
/// jqury.pandochat.js
/// A jQuery plug-in for SignalR-based chat originally built for Pando Logic
/// This depends on the SignalR scripts for the chat hub
/// This also depends on the slimscroll plug-in
///
/// This plug-in is designed to:
/// Use a single instance of the Signal Chat client object to multiplex between multiple chat windows
/// The general process is:
/// 1) Use SignalR client to to start a connection
/// 2) Use SignalR client methods to multiplexing into a collection of chat windows
/// 3) Use jQuery to give chat windows life and enable them to multiplex back to the singleton chat client
/// 4) Do all this wille enabling multiple windows to be live on the user's screen at once
///

///
/// Chat template structure
/// #chat_message_template - Moustache template for the chat message object (see ChatDataEntities.cs)
/// #chat_user_template - Moustache template for the chat user object (see ChatDataEntities.cs)
///
/// Chat box structure
/// .chat-box[data-chat-room-id] - Outside wrapper for the entire chat container, with an attribute indicate chatRoomId
///     .chat-messages - Wrapper for the chat messages
///     .chat-input - the text field for receiving chat messages
///     .chat-button - the button for clicking and sending messages
///     .chat-users - Wrapper for the list of users currently in this chat session
///

(function ($) {

    $.fn.pandochat = function (options) {

        var $allChats = this;

        // options-
        //      currentUserId - A string indicating the current user's ID in the system
        //      chatClient - A SignalR client object able to connect to the SignalR chat hub (This is singleton to the page)

        // TODO: Set default options here
        var settings = $.extend({
        }, options);

        var userId = settings.userId;

        // One-time per page load setup
        var systemMessageTemplate = _.template($("#system_chat_message_template").html());
        var messageTemplate = _.template($("#chat_message_template").html());
        var userTemplate = _.template($("#chat_user_template").html());

        var chatInstances = Object.create(null);
        var userManager = Object.create(null);

        // SignalR inbound multiplex via chatInstances

        // Whenever we receive history, route to the chat instances
        chat = $.connection.chat;
        $.connection.hub.logging = true;

        chat.client.receiveMessage = function (message) {

            var chatInstance = chatInstances[message.ChatRoomId];
            if (chatInstance == undefined)
                return;
            chatInstance.receiveMessage(message);
        };

        // When we receive history, multiplex to the chat instances
        chat.client.receiveHistory = function (history) {

            if (history == null || history == undefined || history.length == 0)
                return;

            var chatInstance = chatInstances[history[0].ChatRoomId];
            if (chatInstance == undefined)
                return;

            chatInstance.receiveHistory(history);
        };

        // Whenever we receive a user list update, route it into the proper message window
        chat.client.receiveUsers = function (response) {

            var chatInstance = chatInstances[response.ChatRoomId];
            if (chatInstance == undefined)
                return;
            chatInstance.receiveUsers(response);
        };

        // Call to start the connection, then wait until it's done to indicate we've joined and start the other processes
        $.connection.hub.start().done(function () {

            // Once the server starts, call the join method once for each chat room on the page activated by this call
            $allChats.each(function () {

                var $chatWrapper = $(this);
                var chatRoomId = $chatWrapper.attr("data-chat-room-id");
                chat.server.join(chatRoomId, options.userId);
            });            
        });

        // Safe for chaining
        return $allChats.each(function () {

            // Parent element is the form, look into it for each field
            var $this = $(this);

            // Pull each field only once for this instance
            var chatRoomId = $this.attr("data-chat-room-id");
            $chatMessages = $this.find(".chat-messages");
            $chatInput = $this.find(".chat-input");
            $chatButton = $this.find(".chat-button");
            $chatUsers = $this.find(".chat-users");

            // Make an object for containing all functions
            var self = {

                applyUserInfoToMessage: function (message) {
                    // Find the user for this message or use a guest stand-in
                    var user = userManager[message.UserId];
                    if (user == undefined || user == null) {
                        user = {
                            Id: "",
                            AvatarUrl: "/Content/images/user-icon.png",
                            FullName: "Guest",
                            FirstName: "",
                            LastName: "",
                            UserUrl: ""
                        };
                    }
                    message = $.extend(user, message);
                    return message;
                },

                // Updates the displayed list of users to match the given response
                receiveUsers: function (response) {

                    $chatUsers.children().remove();

                    for (var i in response.Users) {
                        var data = response.Users[i];
                        var user = userTemplate(data);
                        $user = $(user);
                        $chatUsers.append($user);

                        userManager[data.Id] = data;
                    }
                },

                // Creates an element for the given message
                elementForMessage: function(message) {

                    if (message.Type == 0) {
                        return messageTemplate(message);
                    } else {
                        return systemMessageTemplate(message);
                    }
                },

                // Appends the given message to the chat window
                receiveMessage: function (message) {

                    message = self.applyUserInfoToMessage(message);

                    // apply the message to the screen
                    var msg = self.elementForMessage(message);                    
                    var $msg = $(msg);
                    $chatMessages.append($msg);
                    $chatMessages.animate({ scrollTop: 99999999 }, "slow");
                },

                // Appends the given set of messages to the chat window, immediately scrolling down to them for the user
                receiveHistory: function (history) {

                    for (var index in history) {
                        var message = history[index];
                        message = self.applyUserInfoToMessage(message);
                        var msg = self.elementForMessage(message);
                        var $msg = $(msg);
                        $chatMessages.append($msg);
                    }

                    $chatMessages.attr({ scrollTop: 99999999 });
                },

                // Sends a message using the current state of the chat box
                // This will also blank the chat input field after sending
                sendMessage: function () {
                    var msg = $chatInput.val();
                    chat.server.sendMessage(chatRoomId, userId, msg);
                    $chatInput.val('').focus();
                },

                // Initial UI Setup stuff, using the methods in the object
                // and the variable already pulled out above this object's declaration
                init: function () {

                    // UI Event setup

                    $chatButton.click(function () {
                        self.sendMessage();
                    });

                    $chatInput.keypress(function (e) {
                        if (e.which == 13) {
                            self.sendMessage();
                        }
                    });

                    // Leave the chat room when this page is unloaded
                    $(window).unload(function () {
                        chat.server.leave(chatRoomId, userId);
                    });
                }
            };

            self.init();

            // Add this to the collection of chat instances
            chatInstances[chatRoomId] = self;
        });
    };
}(jQuery));

// slim scroll for chatbox
$('.slimscroll').slimScroll({
    height: '250px'
});