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
    isCustom: boolean,
    customCards: readonly string[],
): Promise<Session> {
    return axios
        .post<Session>(`/api/sessions/${sessionId}/stories`, { title, cardsId, isCustom, customCards })
        .then((r) => r.data);
}

export async function getSession(id: string, cancelToken?: CancelToken): Promise<Session> {
    return axios.get<Session>(`/api/sessions/${id}`, { cancelToken }).then((r) => r.data);
}

export async function getStory(id: string, cancelToken?: CancelToken): Promise<Story> {
    return axios.get<Story>(`/api/stories/${id}`, { cancelToken }).then((r) => r.data);
}

export async function setActiveStory(id: string, storyId: string): Promise<Session> {
    return axios.post<Session>(`/api/sessions/${id}/activestory`, { id: storyId }).then((r) => r.data);
}

export async function vote(id: string, card: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/vote`, { card }).then((r) => r.data);
}

export async function removeVote(id: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/remove_vote`).then((r) => r.data);
}

export async function closeStory(id: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/close`).then((r) => r.data);
}

export async function clearStory(id: string): Promise<Story> {
    return axios.post<Story>(`/api/stories/${id}/clear`).then((r) => r.data);
}

export async function getCards(): Promise<readonly Cards[]> {
    return axios.get<readonly Cards[]>(`/api/Sessions/cards_types`).then((r) => r.data);
}
