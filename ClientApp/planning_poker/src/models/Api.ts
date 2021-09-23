import axios, { CancelToken } from 'axios';
import { Cards, receiveRedirect, saveRedirect, Session, Story, User } from './models';
import { createBrowserHistory } from 'history';

const history = createBrowserHistory({ forceRefresh: true });

axios.defaults.maxRedirects = 0;
axios.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';

axios.interceptors.request.use(
    (request) => {
        const localUser = localStorage.getItem('user');
        if (localUser) {
            const { token } = JSON.parse(localUser) as User;
            request.headers.Authorization = `Bearer ${token}`;
        }
        return request;
    },
    (error) => Promise.reject(error),
);

axios.interceptors.response.use(
    (response) => {
        return response;
    },
    (error) => {
        if (error.response?.status === 302) {
            window.location = error.response.headers.location;
        }

        if (error.response?.status === 401) {
            localStorage.removeItem('user');
            const { from } = receiveRedirect();
            if (from.pathname === '/') {
                saveRedirect({ from: { pathname: history.location.pathname } });
            }
            history.push('/login');
        }
        alert(error.response.statusText);
        return Promise.reject(error);
    },
);

export async function login(name: string): Promise<User> {
    return axios.post<User>('/api/login', { name }).then((r) => r.data);
}

export async function loginGoogle(): Promise<void> {
    return fetch(`/api/login/google-login?returnUrl=${window.location}`, { method: 'GET', redirect: 'manual' })
        .then((r) => {
            if (r.url) {
                window.location.href = r.url;
            }
        })
        .catch((err) => {
            if (err.status == 302) {
                console.log(err.headers.location);
            }
            throw err;
        });
}

export async function createSession(title: string): Promise<Session> {
    return axios.post<Session>('/api/sessions', { title }).then((r) => r.data);
}

export async function createStory(
    sessionId: string,
    title: string,
    cardsId: string | null,
    customCards: readonly string[],
): Promise<Session> {
    return axios
        .post<Session>(`/api/sessions/${sessionId}/stories`, { title, cardsId, customCards })
        .then((r) => r.data);
}

export async function getSession(id: string, cancelToken?: CancelToken): Promise<Session> {
    return axios.get<Session>(`/api/sessions/${id}`, { cancelToken }).then((r) => r.data);
}

export async function getStory(id: string, cancelToken?: CancelToken): Promise<Story> {
    return axios.get<Story>(`/api/stories/${id}`, { cancelToken }).then((r) => r.data);
}

export async function setActiveStory(id: string, storyId: string): Promise<Session> {
    return axios.post<Session>(`/api/sessions/${id}/activestory/${storyId}`).then((r) => r.data);
}

export async function vote(id: string, card: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/vote`, { card }).then((r) => r.data);
}

export async function removeVote(id: string): Promise<Story> {
    return axios.delete<Story>(`/api/stories/${id}/vote`).then((r) => r.data);
}

export async function closeStory(id: string, groups: { [key: string]: readonly string[] }): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/closed`, { groups: groups }).then((r) => r.data);
}

export async function clearStory(id: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/cleared`).then((r) => r.data);
}

export async function getCards(): Promise<readonly Cards[]> {
    return axios.get<readonly Cards[]>(`/api/Sessions/cards_types`).then((r) => r.data);
}

export async function addGroup(id: string, name: string): Promise<Session> {
    return axios.post<Session>(`/api/sessions/${id}/groups`, { name: name }).then((r) => r.data);
}

export async function removeGroup(id: string, groupId: string): Promise<Session> {
    return axios.delete<Session>(`/api/sessions/${id}/groups/${groupId}`).then((r) => r.data);
}

export async function moveParticipantToGroup(id: string, groupId: string, participantId: string): Promise<Session> {
    return axios
        .post<Session>(`/api/sessions/${id}/groups/${groupId}/participants`, { participantId })
        .then((r) => r.data);
}
