import type { RequestCategory, RequestPriority } from "./api/types";

// Accepted, documented duplication: these limits mirror the domain constants
// (MaintenanceRequest.TitleMinLength and friends) only to give instant feedback.
// The server is the authority and validates everything again; its errors are shown too.
export const TITLE_MIN_LENGTH = 5;
export const TITLE_MAX_LENGTH = 120;
export const DESCRIPTION_MIN_LENGTH = 10;
export const DESCRIPTION_MAX_LENGTH = 2000;

export interface CreateRequestFormValues {
  title: string;
  description: string;
  category: RequestCategory | "";
  priority: RequestPriority | "";
}

export type CreateRequestFormErrors = Partial<Record<keyof CreateRequestFormValues, string>>;

/**
 * Length in Unicode code points after trimming, the same measure as the server (runes)
 * and PostgreSQL (char_length): an emoji counts as one character, not two.
 */
export function textLength(value: string): number {
  return [...value.trim()].length;
}

export function validateCreateRequest(values: CreateRequestFormValues): CreateRequestFormErrors {
  const errors: CreateRequestFormErrors = {};

  const titleLength = textLength(values.title);
  if (titleLength < TITLE_MIN_LENGTH || titleLength > TITLE_MAX_LENGTH) {
    errors.title = `El título debe tener entre ${TITLE_MIN_LENGTH} y ${TITLE_MAX_LENGTH} caracteres.`;
  }

  const descriptionLength = textLength(values.description);
  if (descriptionLength < DESCRIPTION_MIN_LENGTH || descriptionLength > DESCRIPTION_MAX_LENGTH) {
    errors.description = `La descripción debe tener entre ${DESCRIPTION_MIN_LENGTH} y ${DESCRIPTION_MAX_LENGTH} caracteres.`;
  }

  if (!values.category) {
    errors.category = "Elige una categoría.";
  }

  if (!values.priority) {
    errors.priority = "Elige una prioridad.";
  }

  return errors;
}
