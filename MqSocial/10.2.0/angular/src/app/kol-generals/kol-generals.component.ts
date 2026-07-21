import { ChangeDetectorRef, Component, Injector, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import {
    KolGeneralServiceProxy,
    KolGeneralDto,
    KolGeneralDtoPagedResultDto,
    KolServiceProxy,
    Bank,
} from '@shared/service-proxies/service-proxies';
import { CreateKolGeneralDialogComponent } from './create-kol-general/create-kol-general-dialog.component';
import { EditKolGeneralDialogComponent } from './edit-kol-general/edit-kol-general-dialog.component';
import { bankLabels } from './bank-labels';
import { Table, TableModule } from 'primeng/table';
import { LazyLoadEvent, PrimeTemplate } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Toolbar } from 'primeng/toolbar';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Tooltip } from 'primeng/tooltip';

@Component({
    templateUrl: './kol-generals.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [
        FormsModule,
        TableModule,
        PrimeTemplate,
        PaginatorModule,
        LocalizePipe,
        Button,
        Toolbar,
        IconField,
        InputIcon,
        InputText,
        Tooltip,
    ],
})
export class KolGeneralsComponent extends PagedListingComponentBase<KolGeneralDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    keyword = '';
    kolNameMap: Record<string, string> = {};

    constructor(
        injector: Injector,
        private _kolGeneralService: KolGeneralServiceProxy,
        private _kolService: KolServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
        this._kolService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((r) => {
            (r.items ?? []).forEach((k) => { this.kolNameMap[k.id] = k.name ?? k.id; });
            this.cd.detectChanges();
        });
    }

    getBankLabel(bank: Bank | undefined): string {
        return bank != null ? (bankLabels[bank] ?? '') : '—';
    }

    getKolNames(kolIds: string[] | undefined): string {
        if (!kolIds || kolIds.length === 0) return '—';
        return kolIds.map((id) => this.kolNameMap[id] ?? id).join(', ');
    }

    createKolGeneral(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateKolGeneralDialogComponent, { class: 'modal-lg' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    editKolGeneral(kolGeneral: KolGeneralDto): void {
        const modalRef: BsModalRef = this._modalService.show(EditKolGeneralDialogComponent, {
            class: 'modal-lg',
            initialState: { id: kolGeneral.id },
        });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._kolGeneralService
            .getAll(
                this.keyword,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: KolGeneralDtoPagedResultDto) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
                this.cd.detectChanges();
            });
    }

    delete(kolGeneral: KolGeneralDto): void {
        abp.message.confirm(
            `Xóa hồ sơ "${kolGeneral.fullName}"?`,
            undefined,
            (result: boolean) => {
                if (result) {
                    this._kolGeneralService.delete(kolGeneral.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
