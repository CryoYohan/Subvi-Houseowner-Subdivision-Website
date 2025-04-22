let allMentions = []; // Stores selected titles
let mentionTitles = [];
let selectedSearchMention = null;
let activeSuggestionIndex = -1;

// Simulate loading
document.addEventListener("DOMContentLoaded", () => {
    fetchAnnouncementTitles();

    setTimeout(() => {
        const loading = document.getElementById('loading-placeholder');
        if (loading) loading.style.display = 'none';
        const posts = document.getElementById('forum-posts');
        if (posts) posts.style.display = 'block';
    }, 1000);
});

function fetchAnnouncementTitles() {
    fetch("/Home/GetAnnouncementTitles")
        .then(res => res.json())
        .then(data => mentionTitles = data);
}

const mentionInput = document.getElementById("mentionInput");
const suggestionBox = document.getElementById("mentionSuggestions");
const hashtagInput = document.getElementById("hashtagHiddenInput");

// Insert badge
function insertMention(title) {
    const selection = window.getSelection();
    const range = selection.getRangeAt(0);

    // Find and remove @mention text node
    const mentionRegex = /@[\w]*$/;
    let node = mentionInput.lastChild;

    while (node) {
        if (node.nodeType === Node.TEXT_NODE) {
            const match = mentionRegex.exec(node.textContent);
            if (match) {
                // Cut off the @mention part
                node.textContent = node.textContent.slice(0, match.index);
                break;
            }
        }
        node = node.previousSibling;
    }

    // Create badge
    const badge = document.createElement("span");
    badge.className = "mention-badge me-1";
    badge.setAttribute("data-title", title);
    badge.setAttribute("contenteditable", "false");
    badge.innerHTML = `${title}<span onclick="removeMentionBadge(this)" class="ms-2" style="cursor:pointer;">&times;</span>`;

    const space = document.createTextNode("\u00A0");

    mentionInput.appendChild(badge);
    mentionInput.appendChild(space);

    allMentions.push(title);
    updateHashtagInput();
    hideMentionSuggestions();
    placeCaretAtEnd(mentionInput);
}


// Remove badge
function removeMentionBadge(span) {
    const badge = span.closest(".mention-badge");
    if (!badge) return;

    const title = badge.getAttribute("data-title");
    allMentions = allMentions.filter(t => t !== title);
    badge.remove();
    updateHashtagInput();

    // If user is currently typing @something, refresh suggestions
    const text = mentionInput.textContent;
    if (/@\w*$/.test(text)) {
        handleTyping(); // manually call
    }
}

// Update hidden input
function updateHashtagInput() {
    hashtagInput.value = allMentions.map(t => `[${t}]`).join(", ");
}

// Handle typing
function handleTyping(e) {
    setTimeout(() => {
        let text = mentionInput.textContent;

        text = text.replace(/\u00A0/g, "").trim();  

        const match = /@(\w*)?$/.exec(text); 
        if (match) {
            const query = (match[1] || "").toLowerCase();

            const matches = mentionTitles.filter(t =>
                t.toLowerCase().includes(query) && !allMentions.includes(t)
            );

            showMentionSuggestions(matches);
        } else {
            hideMentionSuggestions();
        }
    }, 0); 
}

// Show suggestions
function showMentionSuggestions(matches) {
    suggestionBox.innerHTML = "";
    if (matches.length === 0) {
        hideMentionSuggestions();
        return;
    }

    matches.forEach((title, index) => {
        const li = document.createElement("li");
        li.className = "list-group-item list-group-item-action";
        li.textContent = title;
        li.onclick = () => insertMention(title);
        suggestionBox.appendChild(li);
    });

    suggestionBox.classList.remove("d-none");
}

// Hide suggestions
function hideMentionSuggestions() {
    suggestionBox.classList.add("d-none");
    activeSuggestionIndex = -1;
}

function handleKeyDown(e) {
    const allowedKeys = [
        "Backspace", "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown",
        "Delete", "Tab", "Enter"
    ];

    // Always allow navigation and control keys
    if (allowedKeys.includes(e.key) || (e.ctrlKey || e.metaKey)) return;

    // Always allow typing @
    if (e.key === "@") return;

    // Only allow other typing if mention suggestions are visible
    const isMentioning = !mentionSuggestions.classList.contains("d-none");
    if (!isMentioning) {
        e.preventDefault();
    }
}


// Keyboard nav for @ suggestion
function handleBackspace(e) {
    const selection = window.getSelection();
    if (e.key === "Backspace" && selection.rangeCount > 0) {
        const range = selection.getRangeAt(0);
        const currentNode = range.startContainer;
        const prevNode = currentNode.previousSibling;

        const fullText = mentionInput.textContent;
        const isTypingMention = /@[\w]*$/.test(fullText);

        // Don't remove badge if currently typing a mention (e.g., "@so")
        if (isTypingMention) return;

        // Case 1: cursor is after [badge][space]
        if (
            currentNode.nodeType === Node.TEXT_NODE &&
            currentNode.textContent === "\u00A0" &&
            prevNode?.classList?.contains("mention-badge")
        ) {
            e.preventDefault();
            currentNode.remove();
            removeMentionBadge(prevNode.querySelector("span"));
            return;
        }

        // Case 2: cursor is right next to [badge]
        if (prevNode?.classList?.contains("mention-badge")) {
            e.preventDefault();
            removeMentionBadge(prevNode.querySelector("span"));
            return;
        }
    }
}

// Maintain caret
function placeCaretAtEnd(el) {
    el.focus();
    document.execCommand("selectAll", false, null);
    document.getSelection().collapseToEnd();
}

// Clicking inside keeps focus at the end
function focusMentionInput() {
    placeCaretAtEnd(mentionInput);
}

// Toggle Like/Unlike
async function toggleLike(postId) {
    try {
        const response = await fetch(`/home/ToggleLike?postId=${postId}`, { method: 'POST' });
        if (!response.ok) {
            console.error('Failed to toggle like');
            return;
        }

        // Get the elements
        const likeIcon = document.getElementById(`like-icon-${postId}`);
        const likeCount = document.getElementById(`like-count-${postId}`);

        // Read current likes from data-likes (fallback to displayed number if missing)
        let currentLikes = parseInt(likeCount.getAttribute('data-likes')) || parseInt(likeCount.textContent) || 0;

        // Toggle the icon and color
        if (likeIcon.classList.contains('bi-hand-thumbs-up')) {
            likeIcon.classList.remove('bi-hand-thumbs-up', 'text-secondary');
            likeIcon.classList.add('bi-hand-thumbs-up-fill', 'text-primary');
            currentLikes++;
        } else {
            likeIcon.classList.remove('bi-hand-thumbs-up-fill', 'text-primary');
            likeIcon.classList.add('bi-hand-thumbs-up', 'text-secondary');
            if (currentLikes > 0) currentLikes--; // Prevent negative numbers
        }

        // Update like count and display with 'K' format
        likeCount.setAttribute('data-likes', currentLikes);
        likeCount.textContent = formatLikes(currentLikes);
    } catch (error) {
        console.error('Error:', error);
    }
}

// Format likes to display 'K' for thousands
function formatLikes(likes) {
    if (likes >= 1000) {
        return (likes / 1000).toFixed(likes % 1000 !== 0 ? 1 : 0) + 'K';
    }
    return likes;
}

document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById("searchInput");
    const mentionList = document.getElementById("searchMentionSuggestions");

    searchInput.addEventListener("input", (e) => {
        const text = searchInput.innerText; 
        if (text.includes("@") && !selectedSearchMention) {
            showSearchMentionSuggestions();
        }
        updateSearchHiddenInput();
    });

    searchInput.addEventListener("keydown", (e) => {
        if (e.key === "Backspace") {
            const sel = window.getSelection();
            const range = sel.getRangeAt(0);

            if (range.startContainer === searchInput) {
                // If caret is directly after a mention badge
                const nodes = Array.from(searchInput.childNodes);
                const caretIndex = range.startOffset;

                if (caretIndex > 0 && nodes[caretIndex - 1].classList?.contains("badge")) {
                    e.preventDefault(); // prevent default backspace
                    nodes[caretIndex - 1].remove(); // remove badge
                    selectedSearchMention = null;
                    updateSearchHiddenInput();
                    SearchplaceCaretAtEnd(searchInput); // reposition caret
                }
            } else if (range.startContainer.nodeType === Node.TEXT_NODE) {
                const prevSibling = range.startContainer.previousSibling;

                if (prevSibling && prevSibling.classList?.contains("badge") && range.startOffset === 0) {
                    e.preventDefault(); // prevent default backspace
                    prevSibling.remove(); // remove badge
                    selectedSearchMention = null;
                    updateSearchHiddenInput();
                    SearchplaceCaretAtEnd(searchInput); // reposition caret
                }
            }
        }
    });

    document.addEventListener("click", (e) => {
        if (!document.getElementById("search-wrapper").contains(e.target)) {
            mentionList.classList.add("d-none");
        }
    });

    document.getElementById("searchBtn").addEventListener("click", () => {
        const query = document.getElementById("searchHiddenInput").value.trim();
        const mention = document.getElementById("mentionHiddenInput").value.trim();
        const params = new URLSearchParams();
        if (query) params.append("query", query);
        if (mention) params.append("mention", mention);
        
            fetch(`/Home/SearchDiscussions?${params.toString()}`)
                .then(response => response.json())
                .then(data => {
                    const resultsDiv = document.getElementById('forum-posts');
                    const loadingPlaceholder = document.getElementById('loading-placeholder');

                    loadingPlaceholder.style.display = 'none';
                    resultsDiv.style.display = 'block';

                    // Remove previous search message if any
                    const existingMsg = document.getElementById('no-results-msg');
                    if (existingMsg) existingMsg.remove();

                    // Clear only search results, not original content
                    const searchResultsContainer = document.createElement('div');
                    searchResultsContainer.id = 'search-results';

                    if (data.length > 0) {
                        data.forEach(post => {
                            const postElement = document.createElement('div');
                            postElement.classList.add('card', 'mb-3');

                            const likeCount = formatLikes(post.likeCount || 0);
                            const iconClass = post.isLiked ? 'bi-hand-thumbs-up-fill text-primary' : 'bi-hand-thumbs-up text-secondary';

                            const mentions = (post.hashtag || "")
                                .split(',')
                                .map(tag => tag.trim().replace(/\[|\]/g, ''))
                                .filter(tag => tag !== "")
                                .filter((value, index, self) => self.indexOf(value) === index);

                            const mentionsHtml = mentions.map(tag => `
                            <span class="badge bg-light text-primary border border-primary me-2">@${tag}</span>


                        `).join('');
                            const userAvatar = `
                                <div class="user-avatar p-4 text-white rounded-circle d-flex align-items-center justify-content-center">
                                    ${post.profile && post.profile.trim() !== ''
                                    ? `<img src="${post.profile}" alt="${post.fullName}'s Profile Picture" class="img-fluid rounded-circle" style="width: 50px; height: 50px; object-fit: cover;">`
                                    : (post.firstname && post.lastname ? post.firstname[0] + post.lastname[0] : '')
                                }
                                </div>
                            `;

                            postElement.innerHTML = `
                            <div class="card-body">
                                <div class="d-flex align-items-center">
                                    ${userAvatar}
                                    <div class="ms-3">
                                        <h5 class="card-title fw-semibold">${post.title}</h5>
                                        ${mentionsHtml ? `<div class="mb-2">${mentionsHtml}</div>` : ''}
                                        <p class="text-muted mb-1">
                                            Posted by
                                            ${post.role === "Admin" ? `Admin ${post.firstname} ${post.lastname}` : post.fullName} 
                                            on ${new Date(post.datePosted).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}
                                        </p>
                                        <p class="card-text">
                                            ${post.content.length > 100 ? post.content.substring(0, 100) + "..." : post.content}
                                        </p>
                                    </div>
                                </div>
                                <div class="d-flex justify-content-end gap-3">
                                    <button onclick="toggleLike(${post.postId})" class="btn btn-outline-primary btn-sm d-flex align-items-center gap-1">
                                        <i id="like-icon-${post.postId}" class="bi ${iconClass}"></i>
                                        <span id="like-count-${post.postId}" data-likes="${post.likeCount}">${likeCount}</span>
                                    </button>
                                    <a href="/Home/Comments?id=${post.postId}&title=${encodeURIComponent(post.title)}" class="btn btn-outline-secondary btn-sm d-flex align-items-center gap-1">
                                        <i class="bi bi-chat"></i>
                                        <span>${post.repliesDisplay} replies</span>
                                    </a>
                                </div>
                            </div>
                        `;
                            searchResultsContainer.appendChild(postElement);
                        });

                        // Clear previous results and append the new search result content
                        resultsDiv.innerHTML = '';
                        resultsDiv.appendChild(searchResultsContainer);
                    } else {
                        // Only show a message but preserve the original content
                        const message = document.createElement('p');
                        message.id = 'no-results-msg';
                        message.className = 'text-danger text-center fw-semibold mb-3';
                        message.textContent = 'No results found.';

                        resultsDiv.prepend(message);
                    }
                });
        
    });
});

function showSearchMentionSuggestions() {
    const list = document.getElementById("searchMentionSuggestions");
    list.innerHTML = ''; // Clear existing suggestions

    // If user is typing after '@', filter suggestion list based on query
    const query = document.getElementById("searchInput").innerText.trim();
    const mentionSearchQuery = query.substring(query.lastIndexOf("@") + 1); // Get text after '@'

    // Filter announcements that match the mention query
    const filteredTitles = mentionTitles.filter(title => title.toLowerCase().includes(mentionSearchQuery.toLowerCase()));

    // Show suggestions if there are matching titles
    filteredTitles.forEach(title => {
        const li = document.createElement("li");
        li.className = "list-group-item list-group-item-action";
        li.textContent = title;
        li.addEventListener("click", () => {
            insertSearchMention(title); // Insert selected mention
            list.classList.add("d-none"); // Hide suggestion list after selection
        });
        list.appendChild(li);
    });

    // Show or hide suggestion list based on results
    list.classList.toggle("d-none", filteredTitles.length === 0);
}

function insertSearchMention(title) {
    const searchInput = document.getElementById("searchInput");
    const range = window.getSelection().getRangeAt(0);
    const currentText = searchInput.textContent.trim();

    // Remove the last @mention part if it's there
    const mentionRegex = /@[\w]*$/;
    const match = mentionRegex.exec(currentText);
    if (match) {
        const textBeforeMention = currentText.slice(0, match.index);
        searchInput.textContent = textBeforeMention;
    }

    // Create and append badge for selected mention
    const badgeWrapper = document.createElement("span");
    badgeWrapper.className = "badge bg-info text-dark me-2 mb-1 d-inline-flex align-items-center";
    badgeWrapper.setAttribute("contenteditable", "false"); // Non-editable
    badgeWrapper.textContent = title;

    const closeBtn = document.createElement("span");
    closeBtn.innerHTML = "&nbsp;&times;";
    closeBtn.className = "ms-2";
    closeBtn.style.cursor = "pointer";
    closeBtn.addEventListener("click", () => clearSearchMention());

    badgeWrapper.appendChild(closeBtn);

    const spaceAfter = document.createTextNode("\u00A0");

    // Append the badge and space after
    searchInput.appendChild(badgeWrapper);
    searchInput.appendChild(spaceAfter);

    // Set the mention as the selected search mention
    selectedSearchMention = title;
    updateSearchHiddenInput();

    // Place the caret at the end after insertion
    SearchplaceCaretAtEnd(searchInput);
}
function clearSearchMention() {
    const searchInput = document.getElementById("searchInput");

    // Find and remove the mention badge
    const badge = searchInput.querySelector('.badge');
    if (badge) {
        badge.remove();
    }

    // Also remove the whitespace/text node right after the badge (optional) if want to remove extra space
    const nextSibling = searchInput.childNodes[0]?.nextSibling;
    if (nextSibling && nextSibling.nodeType === Node.TEXT_NODE && /^\s*$/.test(nextSibling.nodeValue)) {
        nextSibling.remove();
    }

    // Reset selected mention
    selectedSearchMention = null;
    updateSearchHiddenInput();

    // Place caret at the end
    SearchplaceCaretAtEnd(searchInput);
}
function updateSearchHiddenInput() {
    const searchInput = document.getElementById("searchInput");

    // Clone the input node to avoid altering DOM while cleaning
    const clone = searchInput.cloneNode(true);

    // Remove all badge elements (which represent mentions)
    const badges = clone.querySelectorAll('.badge');
    badges.forEach(badge => badge.remove());

    // Get remaining text only (excluding badge and × icon)
    const cleanQuery = clone.textContent.trim();

    // Get selected mention value if available
    const mention = selectedSearchMention?.trim() ?? "";

    // Set the values to hidden inputs
    document.getElementById("searchHiddenInput").value = cleanQuery;
    document.getElementById("mentionHiddenInput").value = mention;
}

function SearchplaceCaretAtEnd(el) {
    el.focus();
    if (typeof window.getSelection !== "undefined" && typeof document.createRange !== "undefined") {
        const range = document.createRange();
        const sel = window.getSelection();

        // Always place caret after the last child
        if (el.lastChild) {
            range.setStartAfter(el.lastChild);
            range.collapse(true);
            sel.removeAllRanges();
            sel.addRange(range);
        }
    }
}

function updateCharCount() {
    const textarea = document.getElementById('content');
    const charCount = document.getElementById('charCount');
    const errorMessage = document.getElementById('errorMessage');

    // Get the current length of the text in the textarea
    const currentLength = textarea.value.length;

    // Update the character count
    charCount.textContent = `${currentLength}/255`;

    // Show the error message if the limit is reached
    if (currentLength === 255) {
        errorMessage.classList.remove('hidden');
    } else {
        errorMessage.classList.add('hidden');
    }
}

// Disable resizing of the textarea
document.getElementById('content').style.resize = 'none';