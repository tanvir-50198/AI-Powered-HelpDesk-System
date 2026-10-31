// Chat page script. It talks to the REST API:
//   POST /api/chat/conversations
//   POST /api/chat/conversations/{id}/messages
//   POST /api/chat/messages/{id}/feedback
(function () {
    var chatBox = document.getElementById('chat-messages');
    var form = document.getElementById('chat-form');
    var input = document.getElementById('chat-input');
    var sendButton = document.getElementById('chat-send');
    var ticketArea = document.getElementById('ticket-link-area');

    var conversationId = parseInt(chatBox.getAttribute('data-conversation-id'), 10) || 0;

    scrollToBottom();

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        var question = input.value.trim();
        if (question.length === 0) {
            return;
        }
        sendQuestion(question);
    });

    function sendQuestion(question) {
        var emptyText = document.getElementById('chat-empty');
        if (emptyText) {
            emptyText.remove();
        }

        addMessage('User', question);
        input.value = '';
        setBusy(true);

        var thinking = addMessage('Assistant', 'Thinking...');

        ensureConversation()
            .then(function () {
                return postJson('/api/chat/conversations/' + conversationId + '/messages', { question: question });
            })
            .then(function (answer) {
                thinking.remove();
                showAnswer(answer);
            })
            .catch(function (error) {
                thinking.remove();
                showError(error.message, question);
            })
            .then(function () {
                setBusy(false);
                input.focus();
            });
    }

    // Creates the conversation on the first question
    function ensureConversation() {
        if (conversationId > 0) {
            return Promise.resolve();
        }
        return postJson('/api/chat/conversations', {}).then(function (conversation) {
            conversationId = conversation.id;
            chatBox.setAttribute('data-conversation-id', conversationId);
            window.history.replaceState({}, '', '/Chat?id=' + conversationId);
            ticketArea.innerHTML = '<a class="btn btn-sm btn-outline-secondary" href="/Chat/CreateTicket/' +
                conversationId + '">Talk to a human (create ticket)</a>';
        });
    }

    function postJson(url, body) {
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        }).then(function (response) {
            if (response.ok) {
                if (response.status === 204) {
                    return null;
                }
                return response.json();
            }
            if (response.status === 429) {
                throw new Error('You are asking too fast. Please wait a moment and try again.');
            }
            return response.json().catch(function () { return {}; }).then(function (problem) {
                throw new Error(problem.detail || problem.title || 'Something went wrong. Please try again.');
            });
        });
    }

    // ---------- drawing messages ----------
    function addMessage(role, text) {
        var row = document.createElement('div');
        row.className = 'chat-row ' + (role === 'User' ? 'chat-row-user' : 'chat-row-bot');

        var bubble = document.createElement('div');
        bubble.className = 'chat-bubble ' + (role === 'User' ? 'chat-user' : 'chat-bot');

        var textDiv = document.createElement('div');
        textDiv.className = 'chat-text';
        textDiv.textContent = text;          // textContent = safe, no HTML injection
        bubble.appendChild(textDiv);

        row.appendChild(bubble);
        chatBox.appendChild(row);
        scrollToBottom();
        return row;
    }

    function showAnswer(answer) {
        var row = addMessage('Assistant', answer.answer);
        var bubble = row.querySelector('.chat-bubble');

        if (answer.sources && answer.sources.length > 0) {
            var sourceDiv = document.createElement('div');
            sourceDiv.className = 'small mt-2';
            var heading = document.createElement('strong');
            heading.textContent = 'Sources:';
            sourceDiv.appendChild(heading);

            answer.sources.forEach(function (source) {
                var line = document.createElement('div');
                var label = source.title;
                if (source.section) {
                    label += ' > ' + source.section;
                }
                line.textContent = '\u2022 ' + label;
                sourceDiv.appendChild(line);
            });
            bubble.appendChild(sourceDiv);
        }

        if (answer.canCreateTicket) {
            var ticketLink = document.createElement('a');
            ticketLink.className = 'btn btn-sm btn-warning mt-2';
            ticketLink.href = '/Chat/CreateTicket/' + answer.conversationId;
            ticketLink.textContent = 'Create a support ticket';
            bubble.appendChild(ticketLink);
        }
        else {
            bubble.appendChild(createFeedbackButtons(answer.messageId));
        }
        scrollToBottom();
    }

    function createFeedbackButtons(messageId) {
        var box = document.createElement('div');
        box.className = 'mt-2 small';
        box.appendChild(document.createTextNode('Was this helpful? '));

        var yes = document.createElement('button');
        yes.className = 'btn btn-sm btn-outline-success me-1';
        yes.textContent = 'Yes';
        var no = document.createElement('button');
        no.className = 'btn btn-sm btn-outline-danger';
        no.textContent = 'No';

        yes.addEventListener('click', function () { sendFeedback(messageId, true, box); });
        no.addEventListener('click', function () { sendFeedback(messageId, false, box); });

        box.appendChild(yes);
        box.appendChild(no);
        return box;
    }

    function sendFeedback(messageId, isHelpful, box) {
        postJson('/api/chat/messages/' + messageId + '/feedback', { isHelpful: isHelpful, comment: null })
            .then(function () {
                box.textContent = 'Thank you for your feedback!';
            })
            .catch(function (error) {
                box.textContent = error.message;
            });
    }

    function showError(message, question) {
        var row = document.createElement('div');
        row.className = 'chat-row chat-row-bot';

        var bubble = document.createElement('div');
        bubble.className = 'chat-bubble chat-error';
        bubble.textContent = message + ' ';

        var retry = document.createElement('button');
        retry.className = 'btn btn-sm btn-outline-danger ms-2';
        retry.textContent = 'Try again';
        retry.addEventListener('click', function () {
            row.remove();
            sendQuestion(question);
        });
        bubble.appendChild(retry);

        row.appendChild(bubble);
        chatBox.appendChild(row);
        scrollToBottom();
    }

    function setBusy(isBusy) {
        sendButton.disabled = isBusy;
        input.disabled = isBusy;
    }

    function scrollToBottom() {
        chatBox.scrollTop = chatBox.scrollHeight;
    }
})();
